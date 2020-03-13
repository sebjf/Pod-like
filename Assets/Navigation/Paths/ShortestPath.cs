using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortestPath : DerivedPath
{
    public override void Step()
    {
        // brute force search of SP

        for (int i = 0; i < waypoints.Count; i++)
        {
            Minimise(i, DistanceFunction);
        }
    }

    private float DistanceFunction(float d)
    {
        var d1 = Query(d).Midpoint;
        var d2 = Query(d + Resolution).Midpoint;
        var d0 = Query(d - Resolution).Midpoint;
        return (d2 - d1).magnitude + (d1 - d0).magnitude;
    }

    public void Minimise(int i, Function func)
    {
        // which direction reduces the function the most?

        var dw = FiniteDifference(i, func);

        // move by this direction

        waypoints[i].w += -dw * .001f;

        // limit how close to the edges we can get

        var limit = 1f - (Barrier / track.Query(waypoints[i].x).Width);

        waypoints[i].w = Mathf.Clamp(waypoints[i].w, -limit, limit);
    }

    private void Update() // for the enabled flag
    {
        
    }
}
