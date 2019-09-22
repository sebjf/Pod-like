using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(TrackGeometry))]
public class TrackPath : MonoBehaviour
{
    public float Resolution = 5;

    public float Barrier = 3f;

    [SerializeField]
    protected Waypoint[] waypoints;

    [SerializeField]
    protected WaypointsBroadphase1D broadphase;

    [SerializeField]
    protected TrackGeometry track;

    [Serializable]
    public struct Waypoint : IWaypoint1D
    {
        /// <summary>
        /// Position along the Track
        /// </summary>
        public float position;

        /// <summary>
        /// Distance (coefficient) from the Centerline
        /// </summary>
        public float w;

        public float Position => position;
    }

    public void Initialise()
    {
        var track = GetComponent<TrackGeometry>();
        var numWaypoints = Mathf.CeilToInt(track.totalLength / Resolution);
        var trueResolution = track.totalLength / numWaypoints;
        waypoints = new Waypoint[numWaypoints];

        for (int i = 0; i < numWaypoints; i++)
        {
            waypoints[i].position = i * trueResolution;
            waypoints[i].w = 0f;
        }

        // we can initialise here because position never changes, only the width
        broadphase = ScriptableObject.CreateInstance(typeof(WaypointsBroadphase1D)) as WaypointsBroadphase1D;
        broadphase.Initialise(waypoints.Cast<IWaypoint1D>(), track.totalLength);
    }

    public void Step(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Step();
        }
    }

    public virtual void Step()
    {
    }

    protected int mod(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    protected int Get(int i)
    {
        return mod(i, waypoints.Length);
    }

    protected int Next(int i)
    {
        return mod(i + 1, waypoints.Length);
    }
    protected int Prev(int i)
    {
        return mod(i - 1, waypoints.Length);
    }

    public float Curvature(float distance)
    {
        float curvature = 0f;

        var i = broadphase.Evaluate(distance);
        var end = i + 10;
        for (; i < end; i++)
        {
            var wp = Evaluate(waypoints[Get(i)]);
            var prev = Evaluate(waypoints[Prev(i)]);
            var next = Evaluate(waypoints[Next(i)]);

            var tangent = (wp - prev).normalized;
            var actual = (next - wp).normalized;

            var k = (1 - Vector3.Dot(tangent, actual)) * 1f;

            curvature += k;
        }

        return curvature;
    }

    protected Vector3 Evaluate(Waypoint wp)
    {
        return track.Query(wp.position).Position(wp.w);
    }

    public virtual Vector3 Evaluate(float distance)
    {
        var wp = waypoints[broadphase.Evaluate(distance)];
        return track.Query(wp.position).Position(wp.w);
    }

    private void OnDrawGizmos()
    {
        if (waypoints != null)
        {
            if (track == null)
            {
                track = GetComponent<TrackGeometry>();
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < waypoints.Length; i++)
            {
                var wp1 = waypoints[mod(i, waypoints.Length)];
                var wp2 = waypoints[mod(i + 1, waypoints.Length)];

                Gizmos.DrawLine(
                    track.Query(wp1.position).Position(wp1.w),
                    track.Query(wp2.position).Position(wp2.w));

                Gizmos.DrawWireSphere(
                    track.Query(wp1.position).Position(wp1.w),
                    0.5f);
            }
        }
    }



}
