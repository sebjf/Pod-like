using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ResetController))]
[RequireComponent(typeof(Navigator))]
[RequireComponent(typeof(Autopilot))]
[RequireComponent(typeof(PathObservations))]
[RequireComponent(typeof(Vehicle))]

public class ProfileAgent : MonoBehaviour
{
    public int profileLength = 40;
    public int interval = 10;

    private Navigator navigator;
    private ResetController resetController;
    private Autopilot autopilot;
    private PathObservations pathObservations;
    private Vehicle vehicle;

    public float speedStepSize = 5f;
    public float errorThreshold = 1f; // tolerance must be high enough to allow slight corner cutting, since the rabbit is a little ahead of the car

    [HideInInspector]
    public int currentNode;

    [HideInInspector]
    [NonSerialized]
    public bool complete;

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
            speed = 100f;
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
        navigator = GetComponent<Navigator>();
        pathObservations = GetComponent<PathObservations>();
        resetController = GetComponent<ResetController>();
        vehicle = GetComponent<Vehicle>();
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

        if (node > 0) // the car can roll back momentarily in certain cirumstances, such as it starts on a hill
        {
            if (node != prev)   // does the frame straddle an interval?
            {
                if (prev < node) // check we will iterate forwards - if the car spins out, it can move backwards, in which case it will start again
                {
                    while ((prev + 1) != node) // if we've managed to pass multiple nodes within a frame, update the skipped nodes with this frames' data
                    {
                        prev++;
                        UpdateNode(prev);
                    }
                }

                UpdateNode(node);
            }
            if (ComputeError() > errorThreshold)
            {
                UpdateNode(node); // if the car spins out it might not reach the next node so trigger the path error handling code here
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
    }

    private Node Previous(Node node)
    {
        return profile[Util.repeat(profile.IndexOf(node) - 1, profile.Count)];
    }

    private float ComputeError()
    {
        var error = pathObservations.understeer;

        if(pathObservations.jumprulesflag)
        {
            error = 0; // ignore basic lateral error in favour of path geometry bounds

            if (pathObservations.traction)
            {
                if(pathObservations.sideslipAngle >= 20f)
                {
                    error = errorThreshold + 1f;
                }
            }
            else 
            { 
                var position = navigator.GetTrackPosition();
                if (Mathf.Abs(position.offset) > 1.0f)
                {
                    error = errorThreshold + 1f;
                }
            }
        }

        if(pathObservations.PopCollision())
        {
            error = errorThreshold + 1f;
        }

        if ( Vector3.Dot(pathObservations.transform.up, Vector3.up) < 0)
        {
            error = errorThreshold + 1f;
        }

        return error;
    }

    private void UpdateNode(int i)
    {
        var node = profile[i];

        var error = ComputeError();

        node.traction = pathObservations.traction;
        node.actual = pathObservations.speed;
        node.sideslip = pathObservations.sideslipAngle;
        node.error = error;
        node.distance = navigator.PathDistance;

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
        resetController.ResetPosition();
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
            navigator = GetComponent<Navigator>();
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
