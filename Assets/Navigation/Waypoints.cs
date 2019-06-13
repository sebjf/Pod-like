using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class Waypoint   // we can keep waypoint as a class and use references, so long as we are careful not to expect them to remain between serialisation. using class also means we can compare to null.
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
        if(distance >= start && distance < end)
        {
            return true;
        }
        return false;
    }

    public float Evaluate(float distance)
    {
        return (distance - start) / length;
    }
}

public struct Position
{
    public float absolute;
    public float normalised;
    public Vector3 normal;
    public float width;
    public float transverse;
}


public class Waypoints : MonoBehaviour
{
    public List<Waypoint> waypoints = new List<Waypoint>();

    public float totalLength;

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

        totalLength = 0;
        for (int i = 0; i < waypoints.Count; i++)
        {
            var length = Length(waypoints[i]);
            waypoints[i].start = totalLength;
            waypoints[i].end = totalLength + length;
            totalLength += length;
        }
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


    public Waypoint Next(Waypoint current)
    {
        return waypoints[mod(current.index + 1, waypoints.Count)];
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
    public float Evaluate(Vector3 position)
    {
        float smallestDistance = float.MaxValue;
        Waypoint closest = null;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
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

    struct WaypointQuery
    {
        public Waypoint waypoint;
        public float t;
        public float distance;
    }


    private WaypointQuery Query(float distance)
    {
        var trackdistance = mod(distance, totalLength);

        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            if (wp.Contains(trackdistance))
            {
                return new WaypointQuery()
                {
                    distance = trackdistance,
                    waypoint = wp,
                    t = wp.Evaluate(trackdistance)
                };

            }
        }

        throw new Exception("Invalid Distance");
    }

    public Vector3 Midline(float distance)
    {
        var result = Query(distance);
        var wp = result.waypoint;
        var dir = (Next(wp).position - wp.position);
        return wp.position + dir * result.t;
    }

    public float Width(float distance)
    {
        var result = Query(distance);
        var wp = result.waypoint;
        return Mathf.Lerp(wp.width, Next(wp).width, result.t);
    }

    public Vector3 Normal(float distance)
    {
        var result = Query(distance);
        var wp = result.waypoint;
        return Vector3.Lerp(wp.normal, Next(wp).normal, result.t);
    }

    public Vector3 Tangent(float distance)
    {
        var result = Query(distance);
        var wp = result.waypoint;
        return Vector3.Lerp(wp.tangent, Next(wp).tangent, result.t);
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
}
