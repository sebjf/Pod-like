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

public class DerivedPath : Waypoints<DerivedWaypoint>
{
    public float Resolution = 5;
    public float Barrier = 3f;

    [SerializeField]
    protected WaypointsBroadphase1D broadphase;

    [SerializeField]
    protected TrackGeometry track;

    public virtual void Initialise()
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

    public Vector3 Position(float x, float w)
    {
        var T = track.Section(x);
        return T.Position + T.Tangent * w * T.Width * 0.5f;
    }

    public override Vector3 Position(DerivedWaypoint wp)
    {
        return Position(wp.x, wp.w);
    }

    public Vector3 Position(WaypointQueryResult wq)
    {
        return Vector3.Lerp(Position(wq.waypoint), Position(wq.next), wq.t);
    }

    public Vector3 Position(float distance)
    {
        return Position(WaypointQuery(distance));
    }

    public override PathQuery Query(float distance)
    {
        var wq = WaypointQuery(distance);

        var trackdistance = Mathf.Lerp(wq.waypoint.x, wq.next.x, wq.t);        
        var T = track.Query(trackdistance);

        var A = Position(wq.waypoint);
        var B = Position(wq.next);
        var C = Position(Next(wq.next));

        PathQuery query;
        query.Midpoint = Position(wq);

        var a = B - A;
        var b = C - B;

        query.Forward = Vector3.Lerp(a.normalized, b.normalized, wq.t);
        query.Tangent = T.Tangent;
        query.Camber = T.Camber;

        var X = Position(distance + Resolution); // always use the true sample distance, rather than rely on the next/prev waypoints, because they could end up quite close together
        var Z = Position(distance - Resolution);

        query.Curvature = Curvature(X, A, Z);

        query.Inclination = Inclination(query.Forward);

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
        if(!enabled)
        {
            return;
        }
        if (waypoints != null)
        {
            if (track == null)
            {
                track = GetComponentInParent<TrackGeometry>();
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Count; i++)
            {
                var wp1 = Position(Waypoint(i));
                var wp2 = Position(Next(Waypoint(i)));

                Gizmos.DrawLine(
                    wp1,
                    wp2);

                Gizmos.DrawWireSphere(
                    wp1,
                    0.5f);
            }
        }
    }



}
