using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackObservations : MonoBehaviour
{
    private Navigator navigator;

    public int numObservations = 25;
    public float pathInterval = 5;

    public GraphOverlay graph;

    [HideInInspector]
    public float[] Curvature;

    [HideInInspector]
    public float[] Camber;

    [HideInInspector]
    public float[] Inclination;

    [HideInInspector]
    public float[] Distance;

    [HideInInspector]
    public Vector3[] Midpoints;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();

        Curvature = new float[numObservations];
        Camber = new float[numObservations];
        Inclination = new float[numObservations];
        Midpoints = new Vector3[numObservations];
        Distance = new float[numObservations];
    }

    private void FixedUpdate()
    {
        if(Midpoints.Length != numObservations)
        {
            Awake();
        }

        for (int i = 0; i < numObservations; i++)
        {
            var d = navigator.TrackDistance + i * pathInterval;
            var q = navigator.waypoints.Query(d);
            Midpoints[i] = q.Midpoint;
            Camber[i] = q.Camber;
            Curvature[i] = q.Curvature;
            Inclination[i] = q.Inclination;
            Distance[i] = d;
        }
        if (graph) graph.samplesOnScreen = numObservations;
        if (graph) graph.GetSeries("Curvature").values = Curvature.ToList();
        if (graph) graph.GetSeries("Camber").values = Camber.ToList();
        if (graph) graph.GetSeries("Inclination").values = Inclination.ToList();
    }

#if UNITY_EDITOR
    protected void OnDrawGizmosSelected()
    {
        if(navigator == null)
        {
            navigator = GetComponent<Navigator>();
        }

        if (navigator != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < numObservations; i++)
            {
                Gizmos.DrawWireSphere(navigator.waypoints.Query(navigator.TrackDistance + i * pathInterval).Midpoint, 0.25f);
            }
        }
    }
#endif
}
