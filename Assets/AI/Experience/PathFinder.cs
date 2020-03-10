using System;
using System.Collections;
using System.Collections.Generic;
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

    public class Node
    {
        public float speed;
        public float actual;
        public bool traction;
        public float error;
        public int index;
    }

    [HideInInspector]
    [NonSerialized]
    public Node[] profile;

    private void Awake()
    {
        autopilot = GetComponent<Autopilot>();
        navigator = GetComponent<Navigator>();
        pathObservations = GetComponent<PathObservations>();
        resetController = GetComponent<ResetController>();
        resetController.ResetPosition();
        CreateProfile();
    }

    void CreateProfile()
    {
        profile = new Node[profileLength];
        for (int i = 0; i < profile.Length; i++)
        {
            profile[i] = new Node();
            profile[i].speed = 100f;
            profile[i].traction = true;
            profile[i].index = i;
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
        if (next < profileLength && next >= 0)
        {
            autopilot.speed = profile[Mathf.CeilToInt(profileDistance)].speed; // the speed target of the *next* node
        }
    }

    private Node Previous(Node node)
    {
        return profile[node.index - 1];
    }

    private void UpdateNode(int i)
    {
        var node = profile[i];
        var prev = profile[i - 1];

        while(!prev.traction)
        {
            prev = Previous(prev);
        }

        var trueSpeed = pathObservations.speed;
        var traction = pathObservations.traction;
        var error = pathObservations.understeer;

        node.traction = traction;
        node.actual = trueSpeed;
        node.error = error;

        if (traction)
        {
            if (trueSpeed < node.speed - 2)
            {
                node.speed = trueSpeed;
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
        }
        else
        {
            if (trueSpeed > (node.speed + 1))
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
        }

        if(i == (profileLength - 1))
        {
            Debug.Log("Complete!");
            Reset();
        }
    }

    public void ReduceSpeed(Node node)
    {
        node.speed -= Mathf.Min(speedStepSize, node.speed / 2);
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
