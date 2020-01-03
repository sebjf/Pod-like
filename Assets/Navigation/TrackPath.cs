using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(TrackGeometry))]
public class TrackPath : MonoBehaviour, IPath
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

    public int Wrap(int i)
    {
        return mod(i, waypoints.Length);
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
        var i = broadphase.Evaluate(distance);

        var X = Evaluate(waypoints[Next(i)]);
        var Y = Evaluate(waypoints[Get(i)]);
        var Z = Evaluate(waypoints[Prev(i)]);

        var YX = X - Y;
        var YZ = Z - Y;
        var ZY = Y - Z;

        // Compute the direction of the curve

        var c = Mathf.Sign(Vector3.Dot(Vector3.Cross(ZY.normalized, YX.normalized), Vector3.up));

        // https://en.wikipedia.org/wiki/Menger_curvature

        var C = (2f * Mathf.Sin(Mathf.Acos(Vector3.Dot(YX.normalized, YZ.normalized)))) / (X - Z).magnitude;

        C *= c;

        if (float.IsNaN(C))
        {
            C = 0f;
        }

        return C;
    }

    public float Camber(float v)
    {
        var i = broadphase.Evaluate(v);

        var X = Evaluate(waypoints[Next(i)]);
        var Y = Evaluate(waypoints[Get(i)]);
        var Z = Evaluate(waypoints[Prev(i)]);

        var YX = X - Y;
        var YZ = Z - Y;

        return Vector3.Dot(Vector3.Cross(YX.normalized, YZ.normalized), Vector3.up);
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

    public delegate float FunctionToOptimise(float distance);

    public void Minimise(int i, FunctionToOptimise func)
    {
        var stepsize = 0.01f;

        // get gradient of w

        float position = waypoints[i].position;
        float weight = waypoints[i].w;

        var c = func(position);

        waypoints[i].w = weight + stepsize;
        var c0 = func(position);

        waypoints[i].w = weight - stepsize;
        var c1 = func(position);

        waypoints[i].w = weight; // put w back

        // which direction reduces the function the most?

        var step = 0f;
        if (c0 < c1)
        {
            step = stepsize;
        }
        else
        {
            step = -stepsize;
        }

        // move by this direction

        waypoints[i].w += step;

        var limit =  1f - (Barrier / track.Query(position).Width);

        waypoints[i].w = Mathf.Clamp(waypoints[i].w, -limit, limit);
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
