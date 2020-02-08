using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAgentAriadne : VehicleAgent
{
    public override void CollectObservations()
    {
        var observations = GetComponent<TrackObservations>();
        for (int i = 0; i < observations.numObservations; i++)
        {
            AddVectorObs(observations.Curvature[i]);
            AddVectorObs(observations.Camber[i]);
            AddVectorObs(observations.Inclination[i]);
        }

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);

        // num observations: 25 * 3 + 3 = 78
    }

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        var speed = Mathf.Clamp(vectorAction[0], 0, 1);
        speed = speed * 150f;
        pilot.speed = speed;

        AddReward(GetComponent<PathCritic>().reward);
    }
}
