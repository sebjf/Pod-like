using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class Navigator : MonoBehaviour
{
    public TrackPath waypoints;

    public float StartingPosition = -1;

    /// <summary>
    /// Total Distance in Track Space travelled by the car (distance travelled across all laps)
    /// </summary>
    [HideInInspector]
    [NonSerialized]
    public float TotalDistanceTravelled;

    [HideInInspector]
    [NonSerialized]
    public float PreviousTotalDistanceTravelled;

    /// <summary>
    /// Distance around Path in Path Space (the distance along a single lap)
    /// </summary>
    [HideInInspector]
    [NonSerialized]
    public float PathDistance;

    [HideInInspector]
    [NonSerialized]
    public float PreviousPathDistance;

    [HideInInspector]
    [NonSerialized]
    public float distanceTravelledInFrame;

    [HideInInspector]
    [NonSerialized]
    public int Lap;



    private void Start()
    {
        Reset();
    }

    public void Reset()
    {
        if (waypoints == null)
        {
            return;
        }

        Lap = -1;
        PathDistance = waypoints.Distance(transform.position, StartingPosition);
        PreviousPathDistance = PathDistance;
        TotalDistanceTravelled = 0f;
        PreviousTotalDistanceTravelled = 0f;
        distanceTravelledInFrame = 0f;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (waypoints == null)
        {
            return;
        }

        PreviousPathDistance = PathDistance;

        PathDistance = waypoints.Distance(transform.position, PathDistance);

        distanceTravelledInFrame = PathDistance - PreviousPathDistance;

        if(Mathf.Abs(distanceTravelledInFrame) > (waypoints.totalLength / 2))
        {
            // we have crossed over the finish line. figure out which direction so we know if we going forwards or backwards in this frame.

            if(PathDistance < PreviousPathDistance) // forwards
            {
                distanceTravelledInFrame = (waypoints.totalLength - PreviousPathDistance) + PathDistance;
                Lap++;
            }
            if(PathDistance > PreviousPathDistance) // backwards
            {
                distanceTravelledInFrame = -(PreviousPathDistance + (waypoints.totalLength - PathDistance));
                Lap--;
            }
        }

        PreviousTotalDistanceTravelled = TotalDistanceTravelled;
        TotalDistanceTravelled += distanceTravelledInFrame;
    }

    public struct TrackPosition
    {
        public float distance;
        public float offset;
        public float width;
    }

    public TrackPosition GetTrackPosition()
    {
        var section = waypoints.TrackSection(PathDistance);
        var midpoint = (section.left + section.right) * 0.5f;
        var tangent = (section.right - midpoint).normalized;
        var width = (section.right - section.left).magnitude;

        var offset = Vector3.Dot(transform.position - midpoint, tangent) / (width * 0.5f);

        TrackPosition p;
        p.distance = section.trackdistance;
        p.offset = offset;
        p.width = width;

        return p;
    }

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
        {
            FixedUpdate();
        }

        if (waypoints == null)
        {
            waypoints = GetComponentInParent<TrackGeometry>();
        }

        if(waypoints == null)
        {
            return;
        }

        var query = waypoints.Query(PathDistance);

        var midline = query.Midpoint;
        var forward = query.Forward;

        Gizmos.DrawLine(midline, transform.position);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(midline, forward);
    }
}