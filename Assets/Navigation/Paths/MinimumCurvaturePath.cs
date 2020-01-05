using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinimumCurvaturePath : TrackPath
{
    public override void Step()
    {
        float maxcurvature = float.MinValue;
        int index = 0;
        for (int i = 0; i < waypoints.Length; i++)
        {
            var c = Mathf.Abs(Curvature(waypoints[i].position));
            if (c > maxcurvature)
            {
                maxcurvature = c;
                index = i;
            }
        }

        List<int> indicesToUpdate = new List<int>();
        indicesToUpdate.Add(index);

        for (int i = 1; i < 100; i++)
        {
            indicesToUpdate.Add(index + i);
            indicesToUpdate.Add(index - i);
        }

        current = waypoints[index].position;

        foreach (var i in indicesToUpdate)
        {
            Minimise(Wrap(i), CurvatureFunction);
        } 
        
    }

    private float current;

    public float CurvatureFunction(float d)
    {
        return Mathf.Abs(Curvature(current));
    }

    public void Minimise(int i, Function func)
    {
        // which direction reduces the function the most?

        var dw = FiniteDifference(i, func);

        // move by this direction

        waypoints[i].w += -dw;

        // limit how close to the edges we can get

        var limit = 1f - (Barrier / track.Query(waypoints[i].position).Width);

        waypoints[i].w = Mathf.Clamp(waypoints[i].w, -limit, limit);
    }


}
