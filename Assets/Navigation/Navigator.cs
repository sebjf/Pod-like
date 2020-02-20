using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class Navigator : MonoBehaviour
{
    [HideInInspector]
    public TrackGeometry waypoints;

    /// <summary>
    /// Total Distance in Track Space travelled by the car (distance travelled across all laps)
    /// </summary>
    [HideInInspector]
    public float TotalDistanceTravelled;

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
        PreviousTrackDistance = TrackDistance;
        TotalDistanceTravelled = 0f;
        distanceTravelledInFrame = 0f;
    }

    private void Awake()
    {
        waypoints = GetComponentInParent<TrackGeometry>();
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (waypoints == null)
        {
            waypoints = GetComponentInParent<TrackGeometry>();
        }

        if (waypoints == null)
        {
            return;
        }

        waypoints.InitialiseBroadphase();

        TrackDistance = waypoints.Distance(transform.position, TrackDistance);

        distanceTravelledInFrame = TrackDistance - PreviousTrackDistance;

        if (distanceTravelledInFrame < 0 && Mathf.Abs(distanceTravelledInFrame) > (waypoints.totalLength / 2)) // we have crossed over the finish line
        {
            distanceTravelledInFrame = (waypoints.totalLength - PreviousTrackDistance) + TrackDistance;
        }

        TotalDistanceTravelled += distanceTravelledInFrame;

        PreviousTrackDistance = TrackDistance;
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

        var midline = waypoints.Evaluate(TrackDistance);

        Gizmos.DrawLine(midline, transform.position);

        var normal = waypoints.Normal(TrackDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(midline, normal);
    }
}