using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCache
{
    private TrackPath path;
    private float resolution;
    private PathQuery[] table;

    public PathCache(TrackPath path, float resolution)
    {
        this.path = path;
        this.resolution = resolution;

        table = new PathQuery[Mathf.CeilToInt(path.totalLength / resolution)];
        for (float d = 0; d < path.totalLength; d += resolution)
        {
            table[Lower(d)] = path.Query(d);
        }
    }

    private int Lower(float distance)
    {
        return Mathf.FloorToInt(distance / resolution);
    }

    public PathQuery Query(float distance)
    {
        distance = Mathf.Clamp(Util.repeat(distance, path.totalLength), 0, path.totalLength);
        var index = Mathf.FloorToInt(distance / resolution);
        return table[index];
    }
}
