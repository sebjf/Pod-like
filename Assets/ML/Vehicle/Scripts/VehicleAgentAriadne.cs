﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAgentAriadne : VehicleAgent
{
    internal int numObservations = 20;
    internal float pathInterval = 25;

    public override void CollectObservations()
    {
        base.CollectObservations();

        for (int i = 0; i < numObservations; i++)
        {
            AddVectorObs(waypoints.Curvature(navigator.TrackDistance + i * pathInterval));
            AddVectorObs(waypoints.Width(navigator.TrackDistance + i * pathInterval) * 0.01f);
        }

        var tracknormal = waypoints.Normal(navigator.TrackDistance);
        var carnormal = transform.forward;
        AddVectorObs((1f - Vector3.Dot(tracknormal, carnormal)) * Mathf.Sign(Vector3.Dot(Vector3.Cross(tracknormal, carnormal), Vector3.up)));

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);
    }

#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
        if (waypoints != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < numObservations; i++)
            {
                Gizmos.DrawWireSphere(waypoints.Evaluate(navigator.TrackDistance + i * pathInterval, 0f), 0.5f);
            }
        }
    }
#endif
}