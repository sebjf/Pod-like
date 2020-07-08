using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
public class PathObservations : MonoBehaviour
{
    private Navigator navigator;
    private Rigidbody body;
    private Vehicle vehicle;

    private int trackLayerMask;
    private bool collisionOccurred;

    [HideInInspector]
    public float speed;

    [HideInInspector]
    public float sideslipAngle;

    [HideInInspector]
    public float lateralError;

    [HideInInspector]
    public float understeer;

    [HideInInspector]
    public float oversteer;

    [HideInInspector]
    public float height;

    [HideInInspector]
    public bool traction;

    [HideInInspector]
    public float directionError;

    [HideInInspector]
    public float direction;

    [HideInInspector]
    public bool jumprulesflag;


    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
        vehicle = GetComponent<Vehicle>();
        trackLayerMask = LayerMask.GetMask("Track");
        collisionOccurred = false;
    }

    private void Reset()
    {
        collisionOccurred = false;
    }

    void FixedUpdate()
    {
        Profiler.BeginSample("Query");

        var q = navigator.waypoints.Query(navigator.PathDistance);

        var trackCenter = q.Midpoint;
        var trackForward = q.Forward;
        var curvature = q.Curvature;
        var bodyPosition = body.position;

        jumprulesflag = navigator.waypoints.Flags(navigator.PathDistance).jumprules;

        Profiler.EndSample();
        Profiler.BeginSample("Understeer");

        // poor mans projection
        var A = new Vector2(trackCenter.x, trackCenter.z);
        var B = new Vector2(trackCenter.x + trackForward.x, trackCenter.z + trackForward.z);
        var M = new Vector2(bodyPosition.x, bodyPosition.z);
        var T = new Vector2(q.Tangent.x, q.Tangent.z);

        var side = Mathf.Sign((B.x - A.x) * (M.y - A.y) - (B.y - A.y) * (M.x - A.x));

        lateralError = Mathf.Abs(Vector3.Dot(M - A, T));

        if (float.IsInfinity(curvature))
        {
            curvature = -Math.Sign(side); // assume oversteering on perfect straights
        }

        if(side != Mathf.Sign(curvature))
        {
            understeer = 0;
            oversteer = Mathf.Abs(lateralError);
        }
        else
        {
            oversteer = 0;
            understeer = Mathf.Abs(lateralError);
        }

        Profiler.EndSample();
        Profiler.BeginSample("Sideslip Angle");

        sideslipAngle = vehicle.sideslipAngle;

        Profiler.EndSample();
        Profiler.BeginSample("Height");

        height = 0;
        if (vehicle.wheelsInContact < 2)
        {
            RaycastHit raycast;
            if (Physics.Raycast(new Ray(body.position, Vector3.down), out raycast, float.PositiveInfinity, trackLayerMask))
            {
                height = raycast.distance;
            }
        }

        Profiler.EndSample();

        traction = vehicle.wheelsInContact > 3;
        speed = body.velocity.magnitude;

        var projectedVehicleForward = new Vector3(vehicle.transform.forward.x, 0f, vehicle.transform.forward.z).normalized;
        var projectedTrackForward = new Vector3(trackForward.x, 0f, trackForward.z).normalized;

        direction = Vector3.Dot(projectedVehicleForward, projectedTrackForward);
        
        directionError = (1 - Vector3.Dot(projectedTrackForward, projectedVehicleForward)) * Mathf.Sign(Vector3.Dot(Vector3.Cross(projectedTrackForward, projectedVehicleForward), Vector3.up)); 
    }

    /// <summary>
    /// Returns true if a collision has occured since the last time this was called
    /// </summary>
    /// <returns></returns>
    public bool PopCollision()
    {
        if(collisionOccurred)
        {
            collisionOccurred = false;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(Vector3.Dot(collision.GetContact(i).normal, Vector3.up)) < 0.7f)
            {
                collisionOccurred = true;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            if (Mathf.Abs(Vector3.Dot(collision.GetContact(i).normal, Vector3.up)) < 0.7f)
            {
                collisionOccurred = true;
            }
        }
    }
}
