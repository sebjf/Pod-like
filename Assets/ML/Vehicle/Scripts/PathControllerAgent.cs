using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathControllerAgent : VehicleAgent
{
    internal int numObservations = 25;
    internal float pathInterval = 5;

    public override void CollectObservations()
    {
        for (int i = 0; i < numObservations; i++)
        {
            AddVectorObs(waypoints.Curvature(navigator.TrackDistance + i * pathInterval) * 5f);
        }

        var tracknormal = waypoints.Normal(navigator.TrackDistance);
        var carnormal = transform.forward;
        AddVectorObs((1f - Vector3.Dot(tracknormal, carnormal)) * Mathf.Sign(Vector3.Dot(Vector3.Cross(tracknormal, carnormal), Vector3.up)));

        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.01f);

        // num observations: 25 * 2 + 1 + 3 = 54
    }

    Vector3 error;

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        var speed = Mathf.Clamp(vectorAction[0], 0, 1);

        speed = speed * 150f;
        pilot.speed = speed;

        AddReward(-.001f); // gentle negative reward for sitting still
        AddReward((navigator.distanceTravelledInFrame / Time.fixedDeltaTime) / 100f);

        error = transform.position - waypoints.Evaluate(navigator.TrackDistance, target);

        AddReward(-error.magnitude);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if (navigator == null)
        {
            navigator = GetComponent<Navigator>();
        }

        UnityEditor.Handles.BeginGUI();

        GUIStyle style = new GUIStyle();
        string content = "";
        content += "Error: " + error.magnitude;

        UnityEditor.Handles.Label(transform.position, content, style);

        UnityEditor.Handles.EndGUI();
    }

#endif
}
