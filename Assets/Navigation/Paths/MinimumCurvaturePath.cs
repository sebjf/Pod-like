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
            var C = Mathf.Abs(Curvature(waypoints[i].position));
            if (C > maxcurvature)
            {
                maxcurvature = C;
                index = i;
            }
        }

        List<int> indicesToUpdate = new List<int>();
        indicesToUpdate.Add(index);

        for (int i = 1; i < 20; i++)
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

}
