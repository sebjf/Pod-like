using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;


[Serializable]
public class Waypoint : IWaypoint1D  // we can keep waypoint as a class and use references, so long as we are careful not to expect them to remain between serialisation. using class also means we can compare to null.
{
    public int index;

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

    public Vector3 normal
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

    public float length
    {
        get
        {
            return end - start;
        }
    }

    public float start;
    public float end;

    public bool Contains(float distance)
    {
        if(distance >= start && distance <= end)
        {
            return true;
        }
        return false;
    }

    public float Evaluate(float distance)
    {
        return (distance - start) / length;
    }

    public float Position => start;
}

public class TrackGeometry : MonoBehaviour
{
    public List<Waypoint> waypoints = new List<Waypoint>();
    public float totalLength;

    public float curvatureSampleDistance;

    [NonSerialized]
    public WaypointsBroadphase1D broadphase1d;

    [NonSerialized]
    public List<Waypoint> selected = new List<Waypoint>();

    [NonSerialized]
    public List<Waypoint> highlighted = new List<Waypoint>();

    [NonSerialized]
    public Vector3? highlightedPoint;

    public Waypoint lastSelected
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

    public void InitialiseBroadphase()
    {
        if (broadphase1d == null)
        {
            broadphase1d = ScriptableObject.CreateInstance(typeof(WaypointsBroadphase1D)) as WaypointsBroadphase1D;
            broadphase1d.Initialise(waypoints, totalLength);
        }
    }

    public void Add(Waypoint previous, Waypoint waypoint)
    {
        int previousindex = -1;
        if(previous != null)
        {
            previousindex = waypoints.IndexOf(previous);
        }
        waypoints.Insert(previousindex + 1, waypoint);
        Recompute();
    }

    public void Recompute()
    {
        for (int i = 0; i < waypoints.Count; i++)
        {
            waypoints[i].index = i;
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            waypoints[i].normal = Vector3.Lerp((Next(waypoints[i]).position - waypoints[i].position).normalized, (waypoints[i].position - Previous(waypoints[i]).position).normalized, 0.5f);
        }

        totalLength = 0;
        for (int i = 0; i < waypoints.Count; i++)
        {
            var length = Length(waypoints[i]);
            waypoints[i].start = totalLength;
            waypoints[i].end = totalLength + length;
            totalLength += length;
        }

        // some checks

        for (int i = 0; i < waypoints.Count; i++)
        {
            if(waypoints[i].length <= 0)
            {
                Debug.LogError("Degenerate waypoints: " + i);
            }
        }

        broadphase1d = null;
    }

    //https://stackoverflow.com/questions/1082917/mod-of-negative-number-is-melting-my-brain
    private int mod(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    private float mod(float k, float n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    public Waypoint Waypoint(int index)
    {
        return waypoints[mod(index, waypoints.Count)];
    }

    public Waypoint Next(Waypoint current)
    {
        return waypoints[NextIndex(current.index)];
    }

    public int NextIndex(int current)
    {
        return mod(current + 1, waypoints.Count);
    }

    public Waypoint Previous(Waypoint current)
    {
        return waypoints[mod(current.index - 1, waypoints.Count)];
    }

    public Vector3 Tangent(Waypoint current, float t)
    {
        return Vector3.Lerp(current.tangent, Next(current).tangent, t);
    }

    public Vector3 Normal(Waypoint current, float t)
    {
        return Vector3.Lerp(current.normal, Next(current).normal, t);
    }

    public float Width(Waypoint current, float t)
    {
        return Mathf.Lerp(current.width, Next(current).width, t);
    }

    public float Width(float distance)
    {
        return Query(distance).Width;
    }

    /// <summary>
    /// Length of the medial segment between start and the next waypoint
    /// </summary>
    public float Length(Waypoint start)
    {
        return (Next(start).position - start.position).magnitude;
    }

    /// <summary>
    /// Returns the absolute distance along the midline from wp
    /// </summary>
    public float Project(Waypoint wp, Vector3 position)
    {
        return Mathf.Clamp(Vector3.Dot(position - wp.position, (Next(wp).position - wp.position).normalized), 0, wp.length);
    }

    /// <summary>
    /// Returns the absolute distance along the track for the position
    /// </summary>
    public float Distance(Vector3 position, float currentDistance)
    {
        float smallestDistance = float.MaxValue;
        Waypoint closest = null;

        int start = 0;
        int end = waypoints.Count;

        if(currentDistance != -1)
        {
            start = Query(currentDistance).waypoint.index;
            start -= 2;
            end = start + 8;
        } 

        for (int i = start; i < end; i++)
        {
            var wp = Waypoint(i);
            var dir = (Next(wp).position - wp.position).normalized;
            var t = Project(wp, position);
            var projected = wp.position + dir * t;
            var distance = (position - projected).magnitude;

            if(distance < smallestDistance)
            {
                smallestDistance = distance;
                closest = wp;
            }
        }

        return Mathf.Lerp(closest.start, closest.end, Project(closest, position) / closest.length);
    }

    public struct WaypointQuery
    {
        public Waypoint waypoint;
        public Waypoint next;
        public float t;
        public float distance;

        /// <summary>
        /// Position in World Space e across the Track
        /// </summary>
        public Vector3 Position(float e)
        {
            return Midpoint + Tangent * e * Width * 0.5f;
        }

        public Vector3 Midpoint
        {
            get
            {
                return waypoint.position + (next.position - waypoint.position) * t;
            }
        }

        public float Width
        {
            get
            {
                return Mathf.Lerp(waypoint.width, next.width, t);
            }
        }

        public Vector3 Tangent
        {
            get
            {
                return Vector3.Lerp(waypoint.tangent, next.tangent, t);
            }
        }
    }

    public WaypointQuery Query(float distance)
    {
        distance = mod(distance, totalLength);
        distance = Mathf.Clamp(distance, 0, totalLength);

        InitialiseBroadphase();

        var wp = waypoints[broadphase1d.Evaluate(distance)];

        if (!wp.Contains(distance))
        {
            if(Next(wp).Contains(distance))
            {
                wp = Next(wp);
            }
            if(Previous(wp).Contains(distance))
            {
                wp = Previous(wp);
            }
        }

        if(!wp.Contains(distance))
        {
            for (int i = 0; i < waypoints.Count; i++)
            {
                wp = waypoints[i];
                if(wp.Contains(distance))
                {
                    break;
                }
            }
        }

        if (!wp.Contains(distance))
        {
            throw new Exception("Invalid Distance " + distance);
        }

        return new WaypointQuery()
        {
            distance = distance,
            waypoint = wp,
            t = wp.Evaluate(distance),
            next = Next(wp)
        };
    }

    public Vector3 Midline(float distance)
    {
        return Midline(Query(distance));
    }

    public Vector3 Normal(float distance)
    {
        var result = Query(distance);
        var wp = result.waypoint;
        return Vector3.Lerp(wp.normal, Next(wp).normal, result.t);
    }

    public Vector3 Midline(WaypointQuery result)
    {
        var wp = result.waypoint;
        var dir = (Next(wp).position - wp.position);
        return wp.position + dir * result.t;
    }

    public struct Edge
    {
        public Vector3 left;
        public Vector3 right;
    }

    public Edge Edges(float distance)
    {
        var query = Query(distance);

        var mp = query.Midpoint;
        var w = query.Width;
        var t = query.Tangent;
        Edge edge;
        edge.left = mp - t * w * 0.5f;
        edge.right = mp + t * w * 0.5f;
        return edge;
    }

    public IEnumerable<Waypoint> Raycast(Ray ray)
    {
        foreach (var waypoint in waypoints)
        {
            if (((ray.origin + ray.direction * Vector3.Dot(waypoint.position - ray.origin, ray.direction)) - waypoint.position).magnitude < 1f)
            {
                yield return waypoint;
            }
        }
    }

    public float Curvature(float v)
    {
        var X = Query(v + curvatureSampleDistance).Midpoint;
        var Y = Query(v).Midpoint;
        var Z = Query(v - curvatureSampleDistance).Midpoint;

        var YX = X - Y;
        var YZ = Z - Y;
        var ZY = Y - Z;

        // Compute the direction of the curve

        var c = Mathf.Sign(Vector3.Dot(Vector3.Cross(ZY.normalized, YX.normalized), Vector3.up));

        // https://en.wikipedia.org/wiki/Menger_curvature

        var C = (2f * Mathf.Sin(Mathf.Acos(Vector3.Dot(YX.normalized, YZ.normalized)))) / (X - Y).magnitude;

        C *= c;

        if (float.IsNaN(C))
        {
            C = 0f;
        }

        return C;
    }

    public Vector3 Evaluate(float d, float w)
    {
        return Query(d).Position(w);
    }
}
