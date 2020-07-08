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
        if (waypoints.Count < 2)
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
            var length = (Position(Next(waypoints[i])) - Position(waypoints[i])).magnitude;
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