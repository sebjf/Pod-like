using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ResetController))]
[RequireComponent(typeof(Navigator))]
[RequireComponent(typeof(Autopilot))]
[RequireComponent(typeof(PathObservations))]
public class PathFinder : MonoBehaviour
{
    public int profileLength = 40;
    public int interval = 10;

    private Navigator navigator;
    private ResetController resetController;
    private Autopilot autopilot;
    private PathObservations pathObservations;

    public float speedStepSize = 5f;
    public float errorThreshold = 1f; // tolerance must be high enough to allow slight corner cutting, since the rabbit is a little ahead of the car

    [HideInInspector]
    [NonSerialized]
    public bool complete;

    public class Node
    {
        public float speed;
        public float actual;
        public bool traction;
        public float error;
        public float distance; // track position
        public bool mark; // we've intentionally reduced the speed, rather than say the car hitting a bump

        public Node()
        {
            speed = 100f;
            traction = true;
            mark = false;
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
        CreateProfile();
        navigator.Reset();
        resetController.ResetPosition();
    }

    void CreateProfile()
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

        var profileDistance = navigator.TotalDistanceTravelled / interval;
        var previousProfileDistance = navigator.PreviousTotalDistanceTravelled / interval;

        int node = Mathf.FloorToInt(profileDistance);
        var next = Mathf.CeilToInt(profileDistance);

        if (node > 0)
        {
            if (Mathf.FloorToInt(profileDistance) != Mathf.FloorToInt(previousProfileDistance))   // does the frame straddle an interval?
            {
                UpdateNode(node);
            }
        }

        // actuate profile
        if (next < profile.Count && next >= 0)
        {
            autopilot.speed = profile[next].speed; // the speed target of the *next* node
        }
    }

    private Node Previous(Node node)
    {
        return profile[profile.IndexOf(node) - 1];
    }

    private void UpdateNode(int i)
    {
        var trueSpeed = pathObservations.speed;
        var traction = pathObservations.traction;
        var error = pathObservations.understeer;

        var node = profile[i];
        var prev = profile[i - 1];

        while(!prev.traction)
        {
            prev = Previous(prev);
        }

        node.traction = traction;
        node.actual = trueSpeed;
        node.error = error;

        node.distance = navigator.TrackDistance;

        if (traction)
        {
            if (trueSpeed < node.speed - 2)
            {
                node.speed = trueSpeed;
            }
        }

        if (trueSpeed > (node.speed + 1)) // the autopilot will not be perfect, so we must tolerate a tiny offset to avoid pulling prev speed down too much, or for that matter getting stuck where we can't (e.g. at the start)
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

        if (i == (profileLength - 1))
        {
            Debug.Log("Complete!");
            complete = true;
            Reset();
        }
    }

    public void ReduceSpeed(Node node)
    {
        node.speed -= Mathf.Min(speedStepSize, node.speed / 2);
        node.mark = true;
    }

    public void UpdateProfileLength()
    {
        // find the first node with a braking instruction. the derivative will likely be very reliable, but the mark is completely unambiguous.
        int i = profile.IndexOf(profile.First(n => { return n.mark; } ));
        while(profile.Count < (i + profileLength))
        {
            profile.Add(new Node());
        }
        while(profile.Count > (i + profileLength))
        {
            profile.RemoveAt(profile.Count - 1);
        }
    }

    public void Reset()
    {
        resetController.ResetPosition();
    }

#if UNITY_EDITOR
    protected void OnDrawGizmosSelected()
    {
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
