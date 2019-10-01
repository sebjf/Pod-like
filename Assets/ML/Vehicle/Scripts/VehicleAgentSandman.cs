using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class VehicleAgentSandman : VehicleAgent
{
    private AngleAxesObservations trackObservations;

    private void Awake()
    {
        trackObservations = new AngleAxesObservations();
    }

    public override void InitializeAgent()
    {
        base.InitializeAgent();

        trackObservations.Evaluate(navigator);
    }

    public override void CollectObservations()
    {
        Profiler.BeginSample("Collect Observations");

        foreach (var item in trackObservations.samples)
        {
            AddVectorObs(transform.InverseTransformPoint(item.left) * (1f / trackObservations.numIntervals * trackObservations.interval));
            AddVectorObs(transform.InverseTransformPoint(item.right) * (1f / trackObservations.numIntervals * trackObservations.interval));
        }

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);

        Profiler.EndSample();
    }

    private void FixedUpdate()
    {
        trackObservations.Evaluate(navigator);
        pilot.target = trackObservations.EvaluateTargetpoint(target);
    }

   

#if UNITY_EDITOR
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (trackObservations != null)
        {
            Gizmos.color = Color.red;
            foreach (var item in trackObservations.samples)
            {
                Gizmos.DrawWireSphere(item.left, 0.25f);
                Gizmos.DrawWireSphere(item.right, 0.25f);
            }
        }
    }
#endif
}
