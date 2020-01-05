using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortestPath : TrackPath
{
    public override void Step()
    {
        // brute force search of SP

        for (int i = 0; i < waypoints.Length; i++)
        {
            Minimise(i, DistanceFunction);
        }
    }

    private float DistanceFunction(float d)
    {
        return (Evaluate(d + Resolution) - Evaluate(d)).magnitude + (Evaluate(d) - Evaluate(d - Resolution)).magnitude;
    }

    public void Minimise(int i, Function func)
    {
        // which direction reduces the function the most?

        var dw = FiniteDifference(i, func);

        // move by this direction

        waypoints[i].w += -dw * .25f;

        // limit how close to the edges we can get

        var limit = 1f - (Barrier / track.Query(waypoints[i].position).Width);

        waypoints[i].w = Mathf.Clamp(waypoints[i].w, -limit, limit);
    }
}
