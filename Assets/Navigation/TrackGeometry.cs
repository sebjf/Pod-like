using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

[Serializable]
public class Waypoint : IWaypoint1D // we can keep waypoint as a class and use references, so long as we are careful not to expect them to remain between serialisation. using class also means we can compare to null.
{
    public int index;
    public float start;
    public float end;

    public float length
    {
        get
        {
            return end - start;
        }
    }

    public bool Contains(float distance)
    {
        if (distance >= start && distance <= end)
        {
            return true;
        }
        return false;
    }

    public float t(float distance)
    {
        return (distance - start) / length;
    }

    public float Distance => start;
}

public struct PointQuery
{
    public Vector3 Position;
    public Vector3 Tanget;
    public Vector3 Forward;
}

// This single query approach won't make much difference when driving one or two cars, 
// but when collecting experience for 100's (with concomitant cache invalidations) it will.
public struct PathQuery
{
    public Vector3 Midpoint;
    public Vector3 Tangent;
    public Vector3 Forward;
    public float Curvature;
    public float Inclination;
    public float Camber;

    public Vector3 Up
    {
        get
        {
            return Vector3.Cross(Tangent, Forward).normalized;
        }
    }
}

public struct TrackSection
{
    public Vector3 left;
    public Vector3 right;
    public float trackdistance;
    public bool jump;
}

public struct TrackFlags
{
    public bool nospawn;
    public bool jumprules;
}


public abstract class TrackPath : MonoBehaviour
{
    public float totalLength;

    public abstract float Distance(Vector3 position, float lastDistance);

    public abstract PathQuery Query(float distance);
    public abstract TrackFlags Flags(float distance);
    public abstract float TrackDistance(float distance);
    public abstract TrackSection TrackSection(float distance);

    /// <summary>
    /// Computes the curvature of Y. (Where X is *ahead* by h and Z is behind by h.)
    /// </summary>
    public static float Curvature(Vector3 X, Vector3 Y, Vector3 Z)
    {
        // Project onto ground, so we don't include curvature due to inclination, which is a separate property.
        X.y = 0;
        Y.y = 0;
        Z.y = 0;

        var YX = X - Y;
        var YZ = Z - Y;
        var ZY = Y - Z;

        // Compute the direction of the curve
        var direction = Mathf.Sign(Vector3.Dot(Vector3.Cross(ZY.normalized, YX.normalized), Vector3.up));

        // https://en.wikipedia.org/wiki/Menger_curvature
        var curvature = (2f * Mathf.Sin(Mathf.Acos(Vector3.Dot(YX.normalized, YZ.normalized)))) / (X - Z).magnitude;

        curvature *= direction;

        if (float.IsNaN(curvature))
        {
            curvature = 0f;
        }

        return curvature;
    }

    public static float Inclination(Vector3 forward)
    {
        return Vector3.Dot(forward, Vector3.up); // grade is the ratio of height to distance. dot will return the proportion of forward to up/height.
    }

    public virtual string UniqueName()
    {
        return name + " " + GetType().Name;
    }
}

public abstract class Waypoints<T> : TrackPath where T : Waypoint
{
    public List<T> waypoints = new List<T>();

    [NonSerialized]
    public WaypointsBroadphase1D broadphase1d;

    public T Waypoint(int index)
    {
        return waypoints[Util.repeat(index, waypoints.Count)];
    }

    public T Next(T current)
    {
        return waypoints[Next(current.index)];
    }

    public int Next(int current)
    {
        return Util.repeat(current + 1, waypoints.Count);
    }

    public T Previous(T current)
    {
        return waypoints[Previous(current.index)];
    }

    public int Previous(int current)
    {
        return Util.repeat(current - 1, waypoints.Count);
    }

    public void InitialiseBroadphase()
    {
        if (broadphase1d == null)
        {
            broadphase1d = ScriptableObject.CreateInstance(typeof(WaypointsBroadphase1D)) as WaypointsBroadphase1D;
            broadphase1d.Initialise(waypoints, totalLength);
        }
    }

    public struct WaypointQueryResult
    {
        public T waypoint;
        public T next;
        public float t;
        public float distance;
    }

    public WaypointQueryResult WaypointQuery(float distance)
    {
        Profiler.BeginSample("Waypoint Query");

        distance = Mathf.Clamp(Util.repeat(distance, totalLength), 0, totalLength);

        InitialiseBroadphase();

        var wp = waypoints[broadphase1d.Evaluate(distance)];

        if (!wp.Contains(distance))
        {
            if (Next(wp).Contains(distance))
            {
                wp = Next(wp);
            }
            if (Previous(wp).Contains(distance))
            {
                wp = Previous(wp);
            }
        }

        if (!wp.Contains(distance))
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                wp = waypoints[i];
                if (wp.Contains(distance))
                {
                    break;
                }
            }
        }

        if (!wp.Contains(distance))
        {
            throw new Exception("Invalid Distance " + distance);
        }

        Profiler.EndSample();

        return new WaypointQueryResult()
        {
            distance = distance,
            waypoint = wp,
            t = wp.t(distance),
            next = Next(wp)
        };
    }

    /// <summary>
    /// Returns the Euclidean position of wp. This is different to the wp.position member because
    /// Position() may be called before or after Recompute. Recompute calls this to re-initialise
    /// the position member.
    /// When this is called, it is possible to rely on the wp.index however.
    /// </summary>
    public abstract Vector3 Position(T wp);

    public virtual void Recompute()
    {
        if(waypoints.Count < 2)
        {
            return;
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            waypoints[i].index = i;
        }

        totalLength = 0;
        for (int i = 0; i < waypoints.Count; i++)
        {
            var length = (Position(Next(waypoints[i]))- Position(waypoints[i])).magnitude;
            waypoints[i].start = totalLength;
            waypoints[i].end = totalLength + length;
            totalLength += length;
        }

        // some checks

        for (int i = 0; i < waypoints.Count; i++)
        {
            if (waypoints[i].length <= 0)
            {
                Debug.LogError("Degenerate waypoints: " + i);
            }
        }

        broadphase1d = null;
    }

    /// <summary>
    /// Returns the absolute distance along the track for the position
    /// </summary>
    public override float Distance(Vector3 position, float expectedDistance)
    {
        float smallestDistance = float.MaxValue;
        float result = float.NaN;

        int start = 0;
        int end = waypoints.Count;

        if (expectedDistance != -1)
        {
            start = WaypointQuery(expectedDistance).waypoint.index;
            start -= 2;
            end = start + 8;
        }

        for (int i = start; i < end; i++)
        {
            var wp = Waypoint(i);
            var wpposition = Position(wp);
            var nxposition = Position(Next(wp));
            var dir = (nxposition - wpposition).normalized;
            var distanceFoward = Mathf.Clamp(Vector3.Dot(position - wpposition, dir), 0, wp.length); // projection into line between wp and next
            var projected = wpposition + dir * distanceFoward;
            var distance = (position - projected).magnitude;

            if (distance < smallestDistance)
            {
                smallestDistance = distance;
                result = Mathf.Lerp(wp.start, wp.end, distanceFoward / wp.length);
            }
        }

        return result;
    }
}

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

    public Vector3 Forward(TrackWaypoint wp)
    {
        return (Next(wp).position - wp.position).normalized;
    }

    public Vector3 Up(TrackWaypoint wp)
    {
        return Vector3.Cross(Forward(wp), wp.tangent).normalized;
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
