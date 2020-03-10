using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class Navigator : MonoBehaviour
{
    public TrackPath waypoints;

    [HideInInspector]
    public float StartingPosition;

    /// <summary>
    /// Total Distance in Track Space travelled by the car (distance travelled across all laps)
    /// </summary>
    [HideInInspector]
    public float TotalDistanceTravelled;

    [HideInInspector]
    public float PreviousTotalDistanceTravelled;

    /// <summary>
    /// Distance around Track in Track Space (the distance along a single lap)
    /// </summary>
    [HideInInspector]
    public float TrackDistance;

    [HideInInspector]
    public float PreviousTrackDistance;

    [HideInInspector]
    public float distanceTravelledInFrame;

    public void Reset()
    {
        TrackDistance = -1;
        FixedUpdate();
        StartingPosition = TrackDistance;
        PreviousTrackDistance = TrackDistance;
        TotalDistanceTravelled = 0f;
        distanceTravelledInFrame = 0f;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (waypoints == null)
        {
            return;
        }

        PreviousTrackDistance = TrackDistance;

        TrackDistance = waypoints.Distance(transform.position, TrackDistance);

        distanceTravelledInFrame = TrackDistance - PreviousTrackDistance;

        if (distanceTravelledInFrame < 0 && Mathf.Abs(distanceTravelledInFrame) > (waypoints.totalLength / 2)) // we have crossed over the finish line
        {
            distanceTravelledInFrame = (waypoints.totalLength - PreviousTrackDistance) + TrackDistance;
        }

        PreviousTotalDistanceTravelled = TotalDistanceTravelled;
        TotalDistanceTravelled += distanceTravelledInFrame;
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

        var query = waypoints.Query(TrackDistance);

        var midline = query.Midpoint;
        var forward = query.Forward;

        Gizmos.DrawLine(midline, transform.position);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(midline, forward);
    }
}