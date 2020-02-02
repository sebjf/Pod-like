using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAgentAriadne : VehicleAgent
{
    internal int numObservations = 25;
    internal float pathInterval = 5;

    public override void CollectObservations()
    {
        base.CollectObservations();

        for (int i = 0; i < numObservations; i++)
        {
            AddVectorObs(waypoints.Curvature(navigator.TrackDistance + i * pathInterval) * 5f);
        }

        var tracknormal = waypoints.Normal(navigator.TrackDistance);
        var carnormal = transform.forward;
        AddVectorObs((1f - Vector3.Dot(tracknormal, carnormal)) * Mathf.Sign(Vector3.Dot(Vector3.Cross(tracknormal, carnormal), Vector3.up)));

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);

        // num observations: 25 * 1 + 1 + 3 = 29
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        target = Mathf.Clamp(vectorAction[0], -1, 1);
        var speed = Mathf.Clamp(vectorAction[1], 0, 1);

        speed = speed * 150f;
        pilot.speed = speed;

        var pathFollowingError = (waypoints.Evaluate(navigator.TrackDistance) - body.position).magnitude;

        AddReward(-.001f); // gentle negative reward for sitting still
        AddReward(-pathFollowingError);
        AddReward((navigator.distanceTravelledInFrame / Time.fixedDeltaTime) / 100f);
    }

#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
        if (waypoints != null && navigator != null)
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
