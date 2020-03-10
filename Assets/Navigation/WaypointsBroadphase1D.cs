using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public interface IWaypoint1D
{
    float Distance { get; }
}

public class WaypointsBroadphase1D : ScriptableObject
{
    [SerializeField]
    private float resolution;

    [SerializeField]
    private int[] indices;

    [SerializeField]
    private float totalLength;

    public void Initialise(IEnumerable<IWaypoint1D> Waypoints, float totalLength)
    {
        var waypoints = Waypoints.ToList();
        this.totalLength = totalLength;
        resolution = 1f;
        indices = new int[Mathf.CeilToInt(totalLength / resolution)];

        for (int i = 0; i < waypoints.Count; i++)
        {
            int start = Mathf.FloorToInt(waypoints[i].Distance / resolution);

            var length = waypoints[mod(i+1,waypoints.Count)].Distance - waypoints[i].Distance;
            if(length < 0)
            {
                length = totalLength - -length;
            }

            int end = Mathf.CeilToInt((waypoints[i].Distance + length) / resolution);

            for (int c = start; c < end; c++)
            {
                indices[c] = i;
            }
        }
    }
    private int mod(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }
    private float mod(float k, float n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    public int Evaluate(float distance)
    {
        distance = mod(distance, totalLength);
        try
        {
            return indices[Mathf.FloorToInt(distance / resolution)];
        }catch(IndexOutOfRangeException e)
        {
            Debug.Log(distance);
            throw e;
        }
    }
}
