using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(PathNavigator))]
[RequireComponent(typeof(Autopilot))]
[RequireComponent(typeof(PathObservations))]
[RequireComponent(typeof(Vehicle))]

public class ProfileAgent : MonoBehaviour
{
    public int profileLength = 40;
    public int interval = 10;

    private PathNavigator navigator;
    private Autopilot autopilot;
    private PathObservations pathObservations;

    public float speedStepSize = 5f;
    public float errorThreshold = 1f; // tolerance must be high enough to allow slight corner cutting, since the rabbit is a little ahead of the car

    private const float maxSideSlipSum = 20f;

    private float lastUndersteerError;

    [NonSerialized]
    public bool logToConsole;

    [HideInInspector]
    public int currentNode;

    [HideInInspector]
    [NonSerialized]
    public bool complete;

    private class Samples
    {
        private float[] values;
        public Samples(int length)
        {
            values = new float[length];
        }

        public void Add(float value)
        {
            for (int i = 0; i < values.Length - 1; i++) // it is expected the length is small enough that nothing clever than this will end up being any faster...
            {
                values[i] = values[i + 1];
            }
            values[values.Length - 1] = value;
        }

        public float Gradient()
        {
            // if any values are zero, the gradient is invalid
            return 0;
        }
    }

    private float Gradient(float current, float previous)
    {
        if(current == 0)
        {
            return 0;
        }
        if(previous == 0)
        {
            return 0;
        }
        return Mathf.Sign(current - previous);
    }

    public class Node
    {
        public float speed; // target speed
        public float actual; // measured speed
        public bool traction; // we currently have traction
        public float error; // understeer
        public float distance; // track position
        public float sideslip; // sideslip angle sum of wheels with traction
        public bool braking; // the car is actively reducing speed through this section. this is not measured from the autopilot but whether we reduced speed during the MTSA

        public Node()
        {
            speed = 150f;
            traction = true;
            braking = false;
        }
    }

    [HideInInspector]
    [NonSerialized]
    public List<Node> profile;

    private void Awake()
    {
        autopilot = GetComponent<Autopilot>();
        navigator = GetComponent<PathNavigator>();
        pathObservations = GetComponent<PathObservations>();
        logToConsole = false;
        lastUndersteerError = 0f;
        navigator.Reset();
        CreateProfile();
    }

    public void CreateProfile()
    {
        profile = new List<Node>();
        for (int i = 0; i < profileLength; i++)
        {
            profile.Add(new Node());
        }
    }

    private void FixedUpdate()
    {
        // The node update is triggered on the frame the vehicle passes the node distance

        var profileIndex = navigator.TotalDistanceTravelled / interval;
        var previousProfileIndex = navigator.PreviousTotalDistanceTravelled / interval;

        int prev = Mathf.FloorToInt(previousProfileIndex);
        int node = Mathf.FloorToInt(profileIndex);
        var next = Mathf.CeilToInt(profileIndex);

        var error = ComputeError();

        if (node > 0) // the car can roll back momentarily in certain cirumstances, such as it starts on a hill
        {
            if (node != prev)   // does the frame straddle an interval?
            {
                if (prev < node) // check we will iterate forwards - if the car spins out, it can move backwards, in which case it will start again
                {
                    while ((prev + 1) != node) // if we've managed to pass multiple nodes within a frame, update the skipped nodes with this frames' data up till the penultimate one
                    {
                        prev++;
                        UpdateNode(prev, error);
                    }
                }

                UpdateNode(node, error); // update the current node
            }

            if (error > errorThreshold)
            {
                UpdateNode(node, error);  // the error is checked every frame because there are many failure cases that can occur between nodes, even with tight spacing
            }
        }
        else
        {
            if (error > errorThreshold)
            {
                Reset(); // we are not past node 1 but there is an error. the car may have been reset to an invalid state, so reset it again.
            }
        }

        // actuate profile
        if (next < profile.Count && next >= 0)
        {
            autopilot.speed = profile[next].speed; // the speed target of the *next* node
        }

        if(node == 1)
        {
            profile[0].distance = profile[1].distance - interval; // initialise distance of node 0. yes this can be negative, and that is ok because this value is a relative distance not position.
        }

        currentNode = node;

#if UNITY_EDITOR
        if(UnityEditor.Selection.activeGameObject != null)
        {
            logToConsole = UnityEditor.Selection.activeGameObject == this.gameObject;
        }
#endif
    }

    private Node Previous(Node node)
    {
        return profile[Util.repeat(profile.IndexOf(node) - 1, profile.Count)];
    }

    public enum TrackingError
    { 
        OK,
        Understeer,
        JumpTractionLoss,
        JumpExceededBounds,
        Spin,
        Collision,
        Flip
    }

    private void Log(TrackingError error)
    {
        if(logToConsole)
        {
            Debug.Log("PathAgent " + error.ToString());
        }
    }

    private float ComputeError()
    {
        // this function implements the mathematical description of path tracking error. this is quite complicated in order to handle many edge cases.

        if (pathObservations.jumprulesflag)  // in jump rules the error is geometric, based on the track bounds
        {
            if (pathObservations.traction)
            {
                if (pathObservations.sideslipAngle >= maxSideSlipSum)
                {
                    Log(TrackingError.JumpTractionLoss);
                    return errorThreshold + 1f;
                }
            }
            else
            {
                var trackposition = navigator.GetTrackPosition();
                if (Mathf.Abs(trackposition.offset) > 1.0f)
                {
                    Log(TrackingError.JumpExceededBounds);
                    return errorThreshold + 1f;
                }
            }
        }

        if(pathObservations.direction < 0)
        {
            Log(TrackingError.Spin);
            return errorThreshold + 1f;
        }

        if(pathObservations.PopCollision())
        {
            Log(TrackingError.Collision);
            return errorThreshold + 1f;
        }

        if ( Vector3.Dot(pathObservations.transform.up, Vector3.up) < 0)
        {
            Log(TrackingError.Flip);
            return errorThreshold + 1f;
        }

        var error = pathObservations.understeer;
        var errorGradient = Gradient(error, lastUndersteerError);
        lastUndersteerError = error;

        if(errorGradient <= 0)
        {
            error = 0;
        }

        if ( error > errorThreshold)
        {
            Log(TrackingError.Understeer);
        }

        return error;
    }

    private void UpdateNode(int i, float error)
    {
        var node = profile[i];

        node.traction = pathObservations.traction;
        node.actual = pathObservations.speed;
        node.sideslip = pathObservations.sideslipAngle;
        node.error = error;
        node.distance = navigator.Distance;

        var prev = Previous(node);
        while (!prev.traction)
        {
            prev = Previous(prev);
        }

        if (node.actual > (node.speed + 1)) // the autopilot will not be perfect, so we must tolerate a tiny offset to avoid pulling prev speed down too much, or for that matter getting stuck where we can't (e.g. at the start)
        {
            ReduceSpeed(prev);
            Reset();
            return;
        }

        if (error > errorThreshold)
        {
            ReduceSpeed(prev);
            Reset();
            return;
        }

        if (i == (profile.Count - 1))
        {
            Debug.Log("Complete!");
            complete = true;
            Reset();
        }
    }

    public void ReduceSpeed(Node node)
    {
        node.speed = Math.Min(node.actual + 2, node.speed); // only decrease actual if significantly smaller than target speed
        node.speed -= Mathf.Min(speedStepSize, node.speed / 2); // if target speed approaches zero, decrease the step size
        node.braking = true;
    }

    public void Reset()
    {
        ResetController.ResetPosition(navigator);
    }

#if UNITY_EDITOR
    protected void OnDrawGizmosSelected()
    {
        if(UnityEditor.Selection.activeTransform != this.transform)
        {
            return;
        }

        if (navigator == null)
        {
            navigator = GetComponent<PathNavigator>();
        }

        if (navigator != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < profileLength; i++)
            {
                Gizmos.DrawWireSphere(navigator.waypoints.Query(navigator.StartingPosition + i * interval).Midpoint, 0.25f);
            }
        }
    }
#endif
}
