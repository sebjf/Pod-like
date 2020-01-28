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

        navigator.Reset();
        startPosition = navigator.TrackDistance;

        resetOnCollision = FindObjectOfType<VehicleAcademy>().isTraining;
    }

    protected float startPosition;
    protected float target = 0f;

    private void FixedUpdate()
    {
        pilot.target = waypoints.Evaluate(navigator.TrackDistance + 10, target);
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
            AddReward(-1000);
            Done();
            AgentReset();
        }
    }

    public override void AgentReset()
    {
        var academy = GetComponentInParent<VehicleAcademy>();
        ResetPositionOnTrack(startPosition, academy.positionVariation.x, academy.positionVariation.y);
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

        transform.position = waypoints.Evaluate(trackdistance) + Vector3.up * 2;
        transform.forward = waypoints.Normal(trackdistance);

        transform.position = transform.position + (transform.right * UnityEngine.Random.Range(-lateralvariation, lateralvariation));
    }

    public void ClearDone()
    {
        typeof(Agent).GetField("m_Done", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(this, false);
    }
}
