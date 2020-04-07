using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// Centerline path follows the center of the track, except that it smooths corners so no curvature is greater then 10
/// </summary>
public class CenterlinePath : DerivedPath
{
    public float curvature = 1f;

    private float[] curvatures;

    public override void Step()
    {
        // brute force search of SP

        if(curvatures == null)
        {
            curvatures = new float[waypoints.Count];
        }
        if(curvatures.Length != waypoints.Count)
        {
            curvatures = new float[waypoints.Count];
        }

        for (int i = 0; i < waypoints.Count; i++)
        {
            curvatures[i] = CurvatureFunction(waypoints[i].Distance);
        }

        for (int i = 0; i < curvatures.Length; i++)
        {
            if(curvatures[i] > curvature)
            {
                Debug.Log(curvatures[i]);
                Minimise(i, DistanceFunction);
            }
        }

        Debug.Log("Curvature Range " + curvatures.Max());
    }

    private float CurvatureFunction(float d)
    {
        return Mathf.Abs(Query(d).Curvature);
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
