using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAgentAriadne : VehicleAgent
{
    protected ShortestPath path;

    protected int numObservations = 20;
    protected float pathInterval = 25;

    private void Awake()
    {
        path = GetComponentInParent<ShortestPath>();
    }

    public override void CollectObservations()
    {
        base.CollectObservations();

        for (int i = 0; i < numObservations; i++)
        {
            AddVectorObs(path.Curvature(navigator.TrackDistance + i * pathInterval));
        }

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        pilot.target = path.Evaluate(navigator.TrackDistance + 20f);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (path != null)
        {
            var graph = FindObjectOfType<GraphOverlay>();
            if (graph != null)
            {
                graph.widthSeconds = Time.fixedDeltaTime * numObservations;
                var series = graph.GetSeries("Observations");
                series.values.Clear();
                for (int i = 0; i < numObservations; i++)
                {
                    series.values.Add(path.Curvature(navigator.TrackDistance + i * pathInterval));
                }
            }

            Gizmos.color = Color.yellow;
            for (int i = 0; i < numObservations; i++)
            {
                Gizmos.DrawWireSphere(path.Evaluate(navigator.TrackDistance + i * pathInterval), 0.5f);
            }
        }


    }
#endif
}
