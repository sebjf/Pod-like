using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using MLAgents;
using System;

public class VehicleAgent : Agent
{
    protected Rigidbody body;
    protected Autopilot pilot;

    [HideInInspector]
    [NonSerialized]
    public TrackGeometry waypoints;

    [HideInInspector]
    [NonSerialized]
    public Navigator navigator;

    public bool resetOnCollision = true;

    public override void InitializeAgent()
    {
        body = GetComponent<Rigidbody>();
        navigator = GetComponent<Navigator>();
        waypoints = GetComponentInParent<TrackGeometry>();
        pilot = GetComponent<Autopilot>();

        //GetComponent<VehicleControllerInput>().enabled = false;

        navigator.Reset();
        startingDistance = navigator.TrackDistance;

        resetOnCollision = FindObjectOfType<VehicleAcademy>().isTraining;
    }

    protected float startingDistance;
    protected float target = 0.1f;
    protected float speed;
    
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        Profiler.BeginSample("Agent Action");

        target = Mathf.Clamp(vectorAction[0], -1, 1);
        speed = Mathf.Clamp(vectorAction[1], 0, 1);

        speed = speed * 150f;
        pilot.speed = speed;

        AddReward(navigator.distanceTravelledInFrame);

        Profiler.EndSample();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!resetOnCollision)
        {
            return;
        }

        if(!collision.collider.gameObject.CompareTag("Track"))
        {
            return;
        }

        var impulse = transform.InverseTransformVector(collision.impulse.normalized);
        var impulseflat = impulse;
        impulseflat.y = 0;
        impulseflat.Normalize();

        var angle = Mathf.Abs(Mathf.Rad2Deg * Mathf.Acos(Vector3.Dot(impulse, impulseflat)));

        if (angle < 30f)
        {
            Done();
            ResetReward();
            AddReward(-100f);
        }
    }

    public override void AgentReset()
    {
        ResetPositionOnTrack(startingDistance, 25, 4);
        navigator.Reset();
        body.velocity = Vector3.zero;
    }

    public void ResetPositionOnTrack(float trackdistance, float fowardvariation, float lateralvariation)
    {
        if(waypoints == null)   // ResetPositionOnTrack can be called from the editor...
        {
            waypoints = GetComponentInParent<TrackGeometry>();
        }

        trackdistance += UnityEngine.Random.Range(-fowardvariation, fowardvariation); ;

        transform.position = waypoints.Midline(trackdistance) + Vector3.up * 2;
        transform.forward = waypoints.Normal(trackdistance);

        transform.position = transform.position + (transform.right * UnityEngine.Random.Range(-lateralvariation, lateralvariation));
    }

    public void ClearDone()
    {
        typeof(Agent).GetField("m_Done", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(this, false);
    }

#if UNITY_EDITOR
    protected virtual void OnDrawGizmosSelected()
    {
        if(navigator == null)
        {
            navigator = GetComponent<Navigator>();
        }

        UnityEditor.Handles.BeginGUI();
        GUIStyle style = new GUIStyle();

        string content = "";
        content += "Distance: " + navigator.TrackDistance + "\n";
        content += "distanceTravelled: " + navigator.distanceTravelledInFrame + "\n";

        UnityEditor.Handles.Label(transform.position, content, style);

        UnityEditor.Handles.EndGUI();
    }

#endif
}
