using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MinimumCurvaturePath : TrackPath
{
    private float[] gradients;

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

        for (int i = 1; i < 5; i++)
        {
            indicesToUpdate.Add(Wrap(index + i));
            indicesToUpdate.Add(Wrap(index - i));
        }

        Minimise(index, CurvatureMagnitude);

        current = waypoints[index].position;

        foreach (var i in indicesToUpdate)
        {
            Minimise(i, CurvatureFunction);
        } 
        
    }

    private float current;

    public float CurvatureMagnitude(float d)
    {
        return Mathf.Abs(Curvature(d));
    }

    public float CurvatureFunction(float d)
    {
        return Mathf.Abs(Curvature(current, Mathf.Abs(d - current)));
    }

    public void Minimise(int i, Function func)
    {
        // which direction reduces the function the most?

        var dw = FiniteDifference(i, func);

        // move by this direction

        waypoints[i].w += -dw * 0.01f;

        // limit how close to the edges we can get

        var limit = 1f - (Barrier / track.Query(waypoints[i].position).Width);

        waypoints[i].w = Mathf.Clamp(waypoints[i].w, -limit, limit);
    }


}
