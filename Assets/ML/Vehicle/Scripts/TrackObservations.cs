﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackObservations : MonoBehaviour
{
    private Navigator navigator;
    private TrackGeometry waypoints;

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
    public Vector3[] Midpoints;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        waypoints = GetComponentInParent<TrackGeometry>();

        Curvature = new float[numObservations];
        Camber = new float[numObservations];
        Inclination = new float[numObservations];
        Midpoints = new Vector3[numObservations];
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < numObservations; i++)
        {
            var d = navigator.TrackDistance + i * pathInterval;
            var q = navigator.waypoints.Query(d);
            Curvature[i] = waypoints.Curvature(d) * 5f;
            Camber[i] = q.Camber * 10f;
            Inclination[i] = waypoints.Inclination(d) * 1f;
            Midpoints[i] = q.Midpoint;
        }

        if (graph) graph.GetSeries("Curvature", Color.blue).values = Curvature.ToList();
        if (graph) graph.GetSeries("Camber", Color.cyan).values = Camber.ToList();
        if (graph) graph.GetSeries("Inclination", Color.red).values = Inclination.ToList();
    }

#if UNITY_EDITOR
    protected void OnDrawGizmosSelected()
    {
        if(navigator == null)
        {
            navigator = GetComponent<Navigator>();
        }

        if(waypoints == null)
        {
            waypoints = GetComponentInParent<TrackGeometry>();
        }

        if (waypoints != null && navigator != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < numObservations; i++)
            {
                Gizmos.DrawWireSphere(waypoints.Query(navigator.TrackDistance + i * pathInterval).Midpoint, 0.25f);
            }
        }
    }
#endif
}
