using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using MLAgents;

public class VehicleAgent : Agent
{
    private Vehicle vehicle;
    private Rigidbody body;

    private Waypoints waypoints;
    private Navigator navigator;

    private Vector3 startposition;
    private Quaternion startrotation;

    public bool resetOnCollision = true;

    public bool logToConsole = false;

    public override void InitializeAgent()
    {
        vehicle = GetComponent<Vehicle>();
        body = GetComponent<Rigidbody>();
        navigator = GetComponent<Navigator>();
        waypoints = GetComponentInParent<Waypoints>();

        startposition = transform.position;
        startrotation = transform.rotation;

        GetComponent<VehicleControllerInput>().enabled = false;

        previousDistance = 0f;
        totalDistance = 0f;
        startTime = Time.time;

        observations = new Observation[20];
        breadcrumbs = new FixedLengthStack<Vector3>(10);

        navigator.Reset();
        navigator.FixedUpdate();
    }

    public class FixedLengthStack<T> where T : struct
    {
        private T[] stack;
        private int ptr;

        public FixedLengthStack(int length)
        {
            stack = new T[length];
            ptr = 0;
        }

        private int mod(int k, int n)
        {
            return ((k %= n) < 0) ? k + n : k;
        }

        public void Add(T value)
        {
            ptr = mod(++ptr, stack.Length);
            stack[ptr] = value;
        }

        public IEnumerable<T> Items()
        {
            for (int i = 0; i < stack.Length; i++)
            {
                yield return stack[mod(ptr - i, stack.Length)];
            }
        }

        public void Reset()
        {
            for (int i = 0; i < stack.Length; i++)
            {
                stack[i] = default(T);
            }
        }
    }

    private FixedLengthStack<Vector3> breadcrumbs;
    private float breadcrumbTime;
    private float breadcrumbInterval = 0.5f;

    private void CollectBreadcrumbs(Vector3 position)
    {
        if(Time.time > (breadcrumbTime + breadcrumbInterval))
        {
            breadcrumbTime = Time.time;
            breadcrumbs.Add(position);
        }
    }

    public override void CollectObservations()
    {
        Profiler.BeginSample("Collect Observations");

        CollectBreadcrumbs(transform.position);

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.1f);
        AddVectorObs(vehicle.steeringAngle);

        for (int i = 0; i < 15; i++)
        {
            // the waypoints 20 m ahead in 2 m increments along the centreline, normalised by 20 m

            Profiler.BeginSample("Edges Determination");
            var location = navigator.distance + (i * 25);
            location = Mathf.Floor(location / 25) * 25; // round down to nearest 20
            var edges = waypoints.Edges(location);
            Profiler.EndSample();

            observations[i].left = edges.left;
            observations[i].right = edges.right;

            AddVectorObs(transform.InverseTransformPoint(observations[i].left)  / 250f);
            AddVectorObs(transform.InverseTransformPoint(observations[i].right) / 250f);
        }

        foreach (var item in breadcrumbs.Items())
        {
            AddVectorObs(transform.InverseTransformPoint(item) / 200f);
        }

        Profiler.EndSample();
    }

    public struct Observation
    {
        public Vector3 left;
        public Vector3 right;
        public float width;
    }

    Observation[] observations;

    float distance;
    float previousDistance;
    float distanceTravelled;
    float totalDistance;
    float forwardspeed;
    float startTime;
    float angle;
    float margin;
    
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        Profiler.BeginSample("Agent Action");

        var throttle = Mathf.Clamp(vectorAction[0], -1, 1);
        if(throttle < 0)
        {
            vehicle.brake = Mathf.Abs(throttle);
            vehicle.throttle = 0f;
        }
        else
        {
            vehicle.brake = 0f;
            vehicle.throttle = throttle;
        }

        vehicle.steeringAngle = Mathf.Clamp(vectorAction[1], -1, 1);

        distance = navigator.distance;
        distanceTravelled = distance - previousDistance;

        if(distanceTravelled < 0 && Mathf.Abs(distanceTravelled) > (waypoints.totalLength / 2)) // we have crossed over the finish line
        {
            //distanceTravelled = (waypoints.totalLength - previousDistance) + distance;
            distanceTravelled = 1f;
        }

        previousDistance = distance;

        if(distanceTravelled < 0.01)
        {
            AddReward(-10f);
        }
        if(distanceTravelled > 0)
        {
            AddReward(0.1f);
            AddReward(distanceTravelled * 0.1f);
        }

        angle = Vector3.Dot(waypoints.Normal(distance), transform.forward);
        if (angle < 0)
        {
            AddReward(-10f);
        }

        var mp = waypoints.Midline(distance);
        var width = waypoints.Width(distance);
        margin = (width / 2) - (body.position - mp).magnitude;
        if (margin < 2.5)
        {
            AddReward(-10f);
        }

        if (logToConsole)
        {
            Monitor.Log(gameObject.name, vectorAction);
        }

        Profiler.EndSample();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!resetOnCollision)
        {
            return;
        }

        if(!collision.collider.gameObject.CompareTag("Track"))
        {
            return;
        }

        var impulse = transform.InverseTransformVector(collision.impulse.normalized);
        var impulseflat = impulse;
        impulseflat.y = 0;
        impulseflat.Normalize();

        var angle = Mathf.Abs(Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(impulse, impulseflat)));

        if (angle < 30f)
        {
            AddReward(-500f);
            Done();
        }
    }

    public override void AgentReset()
    {
        transform.position = startposition;
        transform.rotation = startrotation;
        body.velocity = Vector3.zero;
        distance = -1f;
        previousDistance = 0f;
        totalDistance = 0f;
        startTime = Time.time;
        navigator.Reset();
        navigator.FixedUpdate();
        breadcrumbs.Reset();
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.BeginGUI();
        GUIStyle style = new GUIStyle();

        string content = "";
        content += "Distance: " + distance + "\n";
        content += "distanceTravelled: " + distanceTravelled + "\n";
        content += "angle: " + angle + "\n";
        content += "margin: " + margin + "\n";

        UnityEditor.Handles.Label(transform.position, content, style);

        UnityEditor.Handles.EndGUI();

        if (observations != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < observations.Length; i++)
            {
                Gizmos.DrawWireSphere(observations[i].left, 1f);
                Gizmos.DrawWireSphere(observations[i].right, 1f);
            }
        }

        if (breadcrumbs != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var item in breadcrumbs.Items())
            {
                Gizmos.DrawWireSphere(item, 1f);
            }
        }
    }

#endif
}
