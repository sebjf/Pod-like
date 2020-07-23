using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

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
    /// Returns the absolute distance along the path for the position
    /// </summary>
    public override float Distance(Vector3 position, float startDistance)
    {
        var start = WaypointQuery(startDistance).waypoint.index;

        Result previous = CheckDistance(position, start);

        for (int i = 1; i < waypoints.Count; i++) // search in both directions sequentially
        {
            if (CheckResult(ref previous, CheckDistance(position, start + i), startDistance))
            {
                break;
            }
        }
        for (int i = 1; i < waypoints.Count; i++)
        {
            if (CheckResult(ref previous, CheckDistance(position, start - i), startDistance))
            {
                break;
            }
        }

        return previous.trackDistance;
    }

    private bool CheckResult(ref Result previous, Result current, float reference)
    {
        var currentTotalDistance = current.lateralDistance;// + ShortestDistance(current.trackDistance, previous.trackDistance);
        var previousTotalDistance = previous.lateralDistance;

        if (currentTotalDistance < previousTotalDistance)
        {
            previous = current;
        }
        else
        {
            return true;
        }

        return false;
    }

    private struct Result
    {
        public float lateralDistance;
        public float trackDistance;
        public bool valid;
    }

    private Result CheckDistance(Vector3 position, int i)
    {
        var wp = Waypoint(i);
        var wpposition = Position(wp);
        var nxposition = Position(Next(wp));
        var dir = (nxposition - wpposition).normalized;
        var a = Vector3.Dot(position - wpposition, dir) / wp.length;

        Result result;
        result.valid = (a >= 0f) && (a <= 1f);

        a = Mathf.Clamp(a, 0, 1);

        var projected = wpposition + dir * a * wp.length;
        var distance = (position - projected).magnitude;

        result.lateralDistance = distance;

        var trackDistance = Mathf.Lerp(wp.start, wp.end, a);

        result.trackDistance = trackDistance;

        if (result.valid)
        {
            var section = track.Section(trackDistance);

            var side = section.right - section.left;
            var e = Vector3.Dot(position - section.left, side.normalized) / side.magnitude;
        }
        
        return result;
    }
}