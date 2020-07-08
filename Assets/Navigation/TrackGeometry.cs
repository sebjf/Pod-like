using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

[Serializable]
public class TrackWaypoint : Waypoint
{
    public Vector3 position
    {
        get
        {
            return Vector3.Lerp(left, right, 0.5f);
        }
        set
        {
            var width = this.width;
            left = value - tangent * width * .5f;
            right = value + tangent * width * .5f;
        }
    }

    public float width
    {
        get
        {
            return (right - left).magnitude;
        }
        set
        {
            var position = this.position;
            left = position - tangent * value * .5f;
            right = position + tangent * value * .5f;
        }
    }

    public Vector3 tangent
    {
        get
        {
            return (right - left).normalized;
        }
        set
        {
            var width = this.width;
            var position = this.position;
            left = position - value * width * .5f;
            right = position + value * width * .5f;
        }
    }

    public Vector3 forward
    {
        get
        {
            return Vector3.Cross(up, tangent).normalized;
        }
        set
        {
            tangent = Vector3.Cross(value, up).normalized;
        }
    }

    public Vector3 up;
    public Vector3 left;
    public Vector3 right;

    public bool nospawn;
    public bool jump;
    public bool jumprules;
}

public class TrackGeometry : Waypoints<TrackWaypoint>
{
    [NonSerialized]
    public List<TrackWaypoint> selected = new List<TrackWaypoint>();

    [NonSerialized]
    public List<TrackWaypoint> highlighted = new List<TrackWaypoint>();

    [NonSerialized]
    public Vector3? highlightedPoint;

    public float curvatureSampleDistance;

    public TrackWaypoint lastSelected
    {
        get
        {
            if(selected.Count > 0)
            {
                return selected.Last();
            }
            return null;
        }
    }

    private void Start()
    {
        InitialiseBroadphase();
    }

    public void Add(TrackWaypoint previous, TrackWaypoint waypoint)
    {
        int previousindex = -1;
        if(previous != null)
        {
            previousindex = waypoints.IndexOf(previous);
        }
        waypoints.Insert(previousindex + 1, waypoint);
        Recompute();
    }

    public override TrackSection TrackSection(float distance)
    {
        var wq = WaypointQuery(distance);
        
        var position = Position(wq);
        var tangent = Tangent(wq);
        var width = Width(wq);
        TrackSection section;
        section.left = position - tangent * width * 0.5f;
        section.right = position + tangent * width * 0.5f;
        section.jump = wq.waypoint.jump;
        section.trackdistance = distance;
        return section;
    }

    public override PathQuery Query(float distance)
    {
        Profiler.BeginSample("TrackGeometry Query");

        var wq = WaypointQuery(distance);
        PathQuery query;

        var A = wq.waypoint;
        var B = wq.next;
        var t = wq.t;

        query.Forward = Vector3.Lerp((B.position - A.position).normalized, (A.position - Previous(A).position).normalized, 0.5f);
        query.Midpoint = Position(wq);
        query.Tangent = Vector3.Lerp(A.tangent, B.tangent, t);

        var width = Mathf.Lerp(A.width, B.width, t);

        query.Camber = query.Tangent.y / width;
        query.Inclination = Inclination(query.Forward);

        var x = Position(WaypointQuery(distance + curvatureSampleDistance));
        var y = query.Midpoint;
        var z = Position(WaypointQuery(distance - curvatureSampleDistance));

        query.Curvature = Curvature(x, y, z);

        Profiler.EndSample();

        return query;
    }

    public override TrackFlags Flags(float distance)
    {
        var wq = WaypointQuery(distance);
        TrackFlags query;
        query.nospawn = wq.waypoint.nospawn;
        query.jumprules = wq.waypoint.jumprules;
        return query;
    }

    public override float TrackDistance(float distance)
    {
        return distance;
    }

    public Vector3 Position(WaypointQueryResult q)
    {
        return Vector3.Lerp(q.waypoint.position, q.next.position, q.t);
    }

    public Vector3 Tangent(WaypointQueryResult q)
    {
        return Vector3.Lerp(q.waypoint.tangent, q.next.tangent, q.t);
    }

    public float Width(WaypointQueryResult q)
    {
        return Mathf.Lerp(q.waypoint.width, q.next.width, q.t);
    }

    public override Vector3 Position(TrackWaypoint wp)
    {
        return wp.position;
    }

    public IEnumerable<TrackWaypoint> Raycast(Ray ray)
    {
        foreach (var waypoint in waypoints)
        {
            if (((ray.origin + ray.direction * Vector3.Dot(waypoint.position - ray.origin, ray.direction)) - waypoint.position).magnitude < 1f)
            {
                yield return waypoint;
            }
        }
    }
}
