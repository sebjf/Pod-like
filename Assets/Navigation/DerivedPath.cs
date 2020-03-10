using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class DerivedWaypoint : Waypoint
{
    /// <summary>
    /// Position along the Track
    /// </summary>
    public float x;

    /// <summary>
    /// Distance (coefficient) from the Centerline
    /// </summary>
    public float w;
}

public class DerivedPath : TrackWaypoints<DerivedWaypoint>
{
    public float Resolution = 5;
    public float Barrier = 3f;

    [SerializeField]
    protected WaypointsBroadphase1D broadphase;

    [SerializeField]
    protected TrackGeometry track;

    public void Initialise()
    {
        var track = GetComponentInParent<TrackGeometry>();
        var numWaypoints = Mathf.CeilToInt(track.totalLength / Resolution);
        var trueResolution = track.totalLength / numWaypoints;

        waypoints.Clear();
        for (int i = 0; i < numWaypoints; i++)
        {
            waypoints.Add(new DerivedWaypoint()
            {
                x = i * trueResolution,
                w = 0f
            });
        }

        Recompute();
    }

    public void Step(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Step();
        }
        Recompute();
    }

    public virtual void Step()
    {
    }

    public override Vector3 Position(DerivedWaypoint wp)
    {
        var T = track.Query(wp.x);
        return T.Midpoint + T.Tangent * wp.w * T.Width * 0.5f;
    }

    public override PathQuery Query(float distance)
    {
        var wp = WaypointQuery(distance);

        var trackdistance = Mathf.Lerp(wp.waypoint.x, wp.next.x, wp.t);        
        var T = track.Query(trackdistance);

        PathQuery query = new PathQuery();
        query.Midpoint = Vector3.Lerp(Position(wp.waypoint), Position(wp.next), wp.t);
        query.Forward = (Position(wp.next) - Position(wp.waypoint)).normalized;
        query.Tangent = T.Tangent;
        query.Camber = T.Camber;
        query.Width = 0;
        
        return query;
    }

    public delegate float Function(float distance);

    /// <summary>
    /// Computes the partial derivative of f(i) with respect to w (the weight or lateral position) using the central difference
    /// </summary>
    public float FiniteDifference(int i, Function f, float h = 0.01f) // of f with respect to w
    {
        var waypoint = waypoints[i];
        float position = waypoint.start;
        float weight = waypoint.w;

        waypoint.w = weight + h * 0.5f;
        var fah1 = f(position);

        waypoint.w = weight - h * 0.5f;
        var fah2 = f(position);

        waypoint.w = weight; // put w back

        return (fah1 - fah2) / h;
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null)
        {
            if (track == null)
            {
                track = GetComponentInParent<TrackGeometry>();
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp1 = Waypoint(i);
                var wp2 = Next(wp1);

                Gizmos.DrawLine(
                    Query(wp1.start).Midpoint,
                    Query(wp2.start).Midpoint);

                Gizmos.DrawWireSphere(
                    Query(wp1.start).Midpoint,
                    0.5f);
            }
        }
    }



}
