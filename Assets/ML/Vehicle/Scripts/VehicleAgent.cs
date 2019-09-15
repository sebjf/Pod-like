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
    private Autopilot pilot;

    private Waypoints waypoints;
    private Navigator navigator;

    public bool resetOnCollision = true;

    public bool logToConsole = false;

    private void Awake()
    {
        trackObservations = new AngleAxesObservations();
        trackObservations.interval = 25;
        trackObservations.numIntervals = 20;
        trackObservations.balance = 0.2f;
        trackObservations.targetSample = (int)(trackObservations.numIntervals * trackObservations.balance + 3);
    }

    public override void InitializeAgent()
    {
        vehicle = GetComponent<Vehicle>();
        body = GetComponent<Rigidbody>();
        navigator = GetComponent<Navigator>();
        waypoints = GetComponentInParent<Waypoints>();
        pilot = GetComponent<Autopilot>();

        //GetComponent<VehicleControllerInput>().enabled = false;

        startTime = Time.time;

        breadcrumbs = new FixedLengthStack<Vector3>(10);

        navigator.Reset();
        navigator.FixedUpdate();
        startingDistance = navigator.distance;
        previousDistance = navigator.distance;
        TotalDistanceTravelled = 0f;

        trackObservations.Evaluate(navigator);

        resetOnCollision = FindObjectOfType<VehicleAcademy>().isTraining;
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
        public AngleAxesObservations()
        {
            samples = new Sample[0];
        }

        public float interval;
        public int numIntervals;
        public float balance;
        public int targetSample;

        public struct Sample
        {
            public Vector3 midpoint;
            public float width;
            public Vector3 trajectory;
            public float angle;
            public Vector3 tangent;
            public Vector3 left;
            public Vector3 right;
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
                var location = navigator.distance - (numIntervals * balance * interval) + (i * interval);
                location = Mathf.Floor(location / interval) * interval; // round down to nearest interval step

                var query = waypoints.Query(location);

                var midpoint = waypoints.Midline(query);
                samples[i].midpoint = midpoint;
                samples[i].width = waypoints.Width(query);
                samples[i].tangent = waypoints.Tangent(query);
                samples[i].left = samples[i].midpoint - samples[i].tangent * samples[i].width * 0.5f;
                samples[i].right = samples[i].midpoint + samples[i].tangent * samples[i].width * 0.5f;

                var next = waypoints.Midline(location + interval);
                var prev = waypoints.Midline(location - interval);

                var trajectory = (next - midpoint).normalized;
                samples[i].trajectory = trajectory;

                var prevTrajectory = (midpoint - prev).normalized;

                samples[i].angle = Mathf.Sign(Vector3.Dot(Vector3.Cross(prevTrajectory, trajectory), Vector3.up)) * Mathf.Acos(Mathf.Clamp(Vector3.Dot(prevTrajectory, trajectory),0f,1f)) * (1f / Mathf.PI);
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
            AddVectorObs(transform.InverseTransformPoint(item.left) * (1f / trackObservations.numIntervals * trackObservations.interval));
            AddVectorObs(transform.InverseTransformPoint(item.right) * (1f / trackObservations.numIntervals * trackObservations.interval));
        }

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);

        Profiler.EndSample();
    }

    public float TotalDistanceTravelled;

    private float startingDistance;
    private float distanceAlongTrack;
    private float previousDistance;
    private float distanceTravelledInFrame;
    private float forwardspeed;
    private float startTime;

    private float target = 0.1f;
    private Vector3 targetPoint;
    private float speed;
    
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        Profiler.BeginSample("Agent Action");

        target = Mathf.Clamp(vectorAction[0], -1, 1);
        speed = Mathf.Clamp(vectorAction[1], 0, 1);

        speed = speed * 150f;
        pilot.speed = speed;

        distanceAlongTrack = navigator.distance;
        distanceTravelledInFrame = distanceAlongTrack - previousDistance;

        if (distanceTravelledInFrame < 0 && Mathf.Abs(distanceTravelledInFrame) > (waypoints.totalLength / 2)) // we have crossed over the finish line
        {
            distanceTravelledInFrame = (waypoints.totalLength - previousDistance) + distanceAlongTrack;
        }

        TotalDistanceTravelled += distanceTravelledInFrame;

        previousDistance = distanceAlongTrack;

        AddReward(distanceTravelledInFrame);

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
            Done();
            ResetReward();
            AddReward(-100f);
        }
    }

    public override void AgentReset()
    {
        ResetPositionOnTrack(startingDistance, 25, 4);
        body.velocity = Vector3.zero;
        startTime = Time.time;
        navigator.Reset();
        navigator.FixedUpdate();
        previousDistance = navigator.distance;
        breadcrumbs.Reset();
        distanceAlongTrack = -1f;
        TotalDistanceTravelled = 0f;
    }

    public void ResetPositionOnTrack(float trackdistance, float fowardvariation, float lateralvariation)
    {
        if(waypoints == null)   // ResetPositionOnTrack can be called from the editor...
        {
            waypoints = GetComponentInParent<Waypoints>();
        }

        trackdistance += Random.Range(-fowardvariation, fowardvariation); ;

        transform.position = waypoints.Midline(trackdistance) + Vector3.up * 2;
        transform.forward = waypoints.Normal(trackdistance);

        transform.position = transform.position + (transform.right * Random.Range(-lateralvariation, lateralvariation));
    }

    private void FixedUpdate()
    {
        trackObservations.Evaluate(navigator);
        pilot.target = trackObservations.EvaluateTargetpoint(target);
    }

    public void ClearDone()
    {
        typeof(Agent).GetField("m_Done", System.Reflection.BindingFlags.NonPublic).SetValue(this, false);
    }

#if UNITY_EDITOR

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.BeginGUI();
        GUIStyle style = new GUIStyle();

        string content = "";
        content += "Distance: " + distanceAlongTrack + "\n";
        content += "distanceTravelled: " + distanceTravelledInFrame + "\n";

        UnityEditor.Handles.Label(transform.position, content, style);

        UnityEditor.Handles.EndGUI();

        if (trackObservations != null)
        {
            Gizmos.color = Color.red;
            foreach (var item in trackObservations.samples)
            {
                Gizmos.DrawWireSphere(item.left, 0.25f);
                Gizmos.DrawWireSphere(item.right, 0.25f);
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
