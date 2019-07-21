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

    private void Awake()
    {
        trackObservations = new AngleAxesObservations();
        trackObservations.interval = 10;
        trackObservations.numIntervals = 15;
        trackObservations.targetSample = 3;
    }

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

    public class AngleAxesObservations
    {
        public float interval;
        public int numIntervals;
        public int targetSample;

        public struct Sample
        {
            public Vector3 midpoint;
            public float width;
            public Vector3 trajectory;
            public float angle;
            public Vector3 tangent;
        }

        public Sample[] samples;

        public void Evaluate(Navigator navigator)
        {
            var waypoints = navigator.waypoints;

            if (samples == null || samples.Length < numIntervals)
            {
                samples = new Sample[numIntervals];
            }

            Profiler.BeginSample("Track Observations");

            for (int i = 0; i < numIntervals; i++)
            {
                var location = navigator.distance + (i * interval);
                location = Mathf.Floor(location / interval) * interval; // round down to nearest interval step

                var query = waypoints.Query(location);

                var midpoint = waypoints.Midline(query);
                samples[i].midpoint = midpoint;
                samples[i].width = waypoints.Width(query);
                samples[i].tangent = waypoints.Tangent(query);

                var next = waypoints.Midline(location + interval);
                var prev = waypoints.Midline(location - interval);

                var trajectory = (next - midpoint).normalized;
                samples[i].trajectory = trajectory;

                var prevTrajectory = (midpoint - prev).normalized;

                samples[i].angle = Vector3.Dot(prevTrajectory, trajectory);
            }

            Profiler.EndSample();
        }

        public Vector3 EvaluateTargetpoint(float w)
        {
            return samples[targetSample].midpoint + samples[targetSample].tangent * w * samples[targetSample].width * 0.5f;
        }
    }


    private FixedLengthStack<Vector3> breadcrumbs;
    private float breadcrumbTime;
    private float breadcrumbInterval = 0.5f;

    private AngleAxesObservations trackObservations;

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

        foreach (var item in trackObservations.samples)
        {
            AddVectorObs(item.angle); // there is a deliberate mistake here
            AddVectorObs(item.width);
        }

        Profiler.EndSample();
    }

    private float distance;
    private float previousDistance;
    private float distanceTravelled;
    private float totalDistance;
    private float forwardspeed;
    private float startTime;
    private float angle;
    private float margin;

    private float target = 0.1f;
    private Vector3 targetPoint;
    
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

    private void FixedUpdate()
    {
        trackObservations.Evaluate(navigator);
        GetComponent<Autopilot>().target = trackObservations.EvaluateTargetpoint(target);
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

        if (trackObservations != null)
        {
            Gizmos.color = Color.red;
            foreach (var item in trackObservations.samples)
            {
                Gizmos.DrawWireSphere(item.midpoint, 0.5f);
                Gizmos.DrawLine(item.midpoint - item.tangent * item.width * 0.5f, item.midpoint + item.tangent * item.width * 0.5f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(trackObservations.EvaluateTargetpoint(target), 1f);
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
