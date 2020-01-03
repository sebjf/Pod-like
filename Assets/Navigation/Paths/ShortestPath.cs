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
}
