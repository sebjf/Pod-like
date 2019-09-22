using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortestPath : TrackPath
{
    public override void Step()
    {
        var track = GetComponent<TrackGeometry>();

        // brute force search of SP

        var stepsize = 0.01f;

        for (int i = 0; i < waypoints.Length; i++)
        {
            // gradient of path length w.r.t edge
            var next = waypoints[Next(i)];
            var curr = waypoints[i];
            var prev = waypoints[Prev(i)];

            // algorithm is sort of like FABRIK
            // project the point onto the line connecting the siblings (to minimise distance), so long as the point doesn't exit the track
            // (because the point is defined in track space, that last part is just clamping it to -1..1)

            // should the point move towards the midline or further away to reduce curvature?

            var q = track.Query(curr.position);

            var x1 = track.Query(next.position).Position(next.w);
            var x0 = track.Query(prev.position).Position(prev.w);

            var xm = x0 + Vector3.Dot(q.Position(curr.w) - x0, (x1 - x0).normalized) * (x1 - x0).normalized;

            var intersection = GeometricTests.ClosestPointSegmentRay(new GeometricTests.Segment()
            {
                p = x0,
                q = x1
            },
            new Ray()
            {
                origin = q.Midpoint,
                direction = q.Tangent
            }
            );

            //  Debug.DrawLine(x0, x1, Color.red, 100);
            //  Debug.DrawLine(q.Midpoint, q.Midpoint - q.Tangent * 20, Color.yellow, 100);

            xm = intersection.c1;

            // get xm as w
            var pw = Mathf.Sign(Vector3.Dot(xm - q.Midpoint, q.Tangent)) * (xm - q.Midpoint).magnitude / (q.Width * 0.5f);

            var limit = 1 - (Barrier / (q.Width * 0.5f));

            waypoints[i].w = pw;
            waypoints[i].w = Mathf.Clamp(waypoints[i].w, -limit, limit);
        }
    }
}
