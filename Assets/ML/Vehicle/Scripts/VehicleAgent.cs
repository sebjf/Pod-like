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
    protected ResetController reset;

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
        reset = GetComponent<ResetController>();

        resetOnCollision = FindObjectOfType<VehicleAcademy>().isTraining;
    }

    protected float target = 0f;

    private void FixedUpdate()
    {
        pilot.target = target;
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
        reset.lateralvariation = academy.positionVariation.y;
        reset.forwardvariation = academy.positionVariation.x;
        reset.ResetPosition();
    }

    public void ClearDone()
    {
        typeof(Agent).GetField("m_Done", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(this, false);
    }
}
