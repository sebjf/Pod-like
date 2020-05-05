using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShortestPath : DerivedPath
{
    public void Step()
    {
        // brute force search of SP
        for (int i = 0; i < waypoints.Count; i++)
        {
            Minimise(i, DistanceFunction);
        }
    }

    public void Step(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Step();
        }
        Recompute();
    }

    private float DistanceFunction(float d)
    {


        var d1 = Position(d);
        var d2 = Position(d + Resolution);
        var d0 = Position(d - Resolution);
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

    public delegate float Function(float distance);

    /// <summary>
    /// Computes the partial derivative of f(i) with respect to w (the weight or lateral position) using the central difference
    /// </summary>
    public float FiniteDifference(int i, Function f, float h = 0.01f) // of f with respect to w
    {
        var waypoint = waypoints[i];
        float position = waypoint.start;
        float weight = waypoint.w;

        waypoint.w = weight + h * 0.5f;
        var fah1 = f(position);

        waypoint.w = weight - h * 0.5f;
        var fah2 = f(position);

        waypoint.w = weight; // put w back

        return (fah1 - fah2) / h;
    }

}
