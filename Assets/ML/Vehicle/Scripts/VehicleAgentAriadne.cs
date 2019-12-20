using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAgentAriadne : VehicleAgent
{
    protected int numObservations = 20;
    protected float pathInterval = 25;

    public override void CollectObservations()
    {
        base.CollectObservations();

        for (int i = 0; i < numObservations; i++)
        {
            AddVectorObs(waypoints.Curvature(navigator.TrackDistance + i * pathInterval));
            AddVectorObs(waypoints.Width(navigator.TrackDistance + i * pathInterval));
        }

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);
    }

#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
        if (FindObjectOfType<DriftCamera>().Target == gameObject.GetComponent<CamRig>())
        {
            if (waypoints != null)
            {
                var graph = FindObjectOfType<GraphOverlay>();
                if (graph != null)
                {
                    graph.widthSeconds = Time.fixedDeltaTime * numObservations;
                    var series = graph.GetSeries("Observations");
                    series.values.Clear();
                    for (int i = 0; i < numObservations; i++)
                    {
                        series.values.Add(waypoints.Curvature(navigator.TrackDistance + i * pathInterval));
                    }
                }

                Gizmos.color = Color.yellow;
                for (int i = 0; i < numObservations; i++)
                {
                    Gizmos.DrawWireSphere(waypoints.Evaluate(navigator.TrackDistance + i * pathInterval, 0f), 0.5f);
                }
            }
        }
    }
#endif
}
