using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public abstract class Navigator : MonoBehaviour
{
    public TrackPath waypoints;
    public float StartingPosition = -1;

    public virtual Vector3 position
    {
        get { return transform.position; }
    }

    /// <summary>
    /// Total Distance in Track Space travelled by the car (distance travelled across all laps)
    /// </summary>
    [NonSerialized]
    public float TotalDistanceTravelled;

    [NonSerialized]
    public float PreviousTotalDistanceTravelled;

    /// <summary>
    /// Distance around Path in Path Space (the distance along a single lap)
    /// </summary>
    [NonSerialized]
    public float Distance;

    [NonSerialized]
    public float PreviousDistance;

    [NonSerialized]
    public int Lap;

    private void Start()
    {
        Reset();
    }

    public virtual void Reset()
    {
        if (waypoints == null)
        {
            return;
        }

        Distance = waypoints.Distance(position, StartingPosition);
        PreviousDistance = Distance;
        TotalDistanceTravelled = 0f;
        PreviousTotalDistanceTravelled = 0f;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if (waypoints == null)
        {
            return;
        }

        PreviousDistance = Distance;

        Distance = waypoints.Distance(position, Distance);

        var distanceTravelledInFrame = Distance - PreviousDistance;

        if(Mathf.Abs(distanceTravelledInFrame) > (waypoints.totalLength / 2))
        {
            // we have crossed over the finish line. figure out which direction so we know if we going forwards or backwards in this frame.

            if(Distance < PreviousDistance) // forwards
            {
                distanceTravelledInFrame = (waypoints.totalLength - PreviousDistance) + Distance;
                Lap++;
            }
            if(Distance > PreviousDistance) // backwards
            {
                distanceTravelledInFrame = -(PreviousDistance + (waypoints.totalLength - Distance));
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
        var section = waypoints.track.Section(waypoints.TrackDistance(Distance));
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

    protected virtual void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            FixedUpdate();
        }

        if (waypoints == null)
        {
            waypoints = GetComponentInParent<TrackGeometry>();
        }

        if (waypoints == null)
        {
            return;
        }

        var query = waypoints.Query(Distance);

        var midline = query.Midpoint;
        var forward = query.Forward;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(midline, position);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(midline, forward);
    }
}