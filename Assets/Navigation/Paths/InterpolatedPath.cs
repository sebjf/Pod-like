using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Always between SP and MCP
/// </summary>
public class InterpolatedPath : DerivedPath
{
    public float coefficient = 0.5f;

    [SerializeField]
    public float[] coefficients;

    private ShortestPath sp;
    private MinimumCurvaturePath mcp;

    public List<int> crossoverPoints;

    private void Awake()
    {
        sp = GetComponentInParent<ShortestPath>();
        mcp = GetComponentInParent<MinimumCurvaturePath>();
        _track = GetComponentInParent<TrackGeometry>();
    }

    private void TryAwake()
    {
        if (sp == null)
        {
            Awake();
        }
        if (mcp == null)
        {
            Awake();
        }
        if(track == null)
        {
            Awake();    
        }
    }

    public override void Initialise()
    {
        TryAwake();

        waypoints.Clear();
        for (int i = 0; i < sp.waypoints.Count; i++)
        {
            waypoints.Add(new DerivedWaypoint()
            {
                x = sp.waypoints[i].x
            });
        }

        coefficients = new float[waypoints.Count];

        for (int i = 0; i < coefficients.Length; i++)
        {
            coefficients[i] = coefficient;
        }

        Recompute();
    }

    public override void Recompute()
    {
        TryAwake();

        for (int i = 0; i < waypoints.Count; i++)
        {
            waypoints[i].w = coefficients[i] * mcp.waypoints[i].w + (1 - coefficients[i]) * sp.waypoints[i].w;
        }

        FindCrossoverPoints();

        base.Recompute();
    }

    public void FindCrossoverPoints()
    {
        crossoverPoints = new List<int>();
        TryAwake();

        var expected = sp.waypoints[0].w < mcp.waypoints[0].w;

        for (int i = 0; i < waypoints.Count; i++)
        {
            var actual = (sp.waypoints[i].w < mcp.waypoints[i].w);
            if(actual != expected)
            {
                expected = actual;
                crossoverPoints.Add(i);
            }
        }
    }

    public override string UniqueName()
    {
        return name + " " + "Interpolated " + coefficient.ToString();
    }
}
