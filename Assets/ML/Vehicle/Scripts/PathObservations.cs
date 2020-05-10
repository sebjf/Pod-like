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

        var q = navigator.waypoints.Query(navigator.TrackDistance);

        var trackCenter = q.Midpoint;
        var trackForward = q.Forward;
        var curvature = q.Curvature;
        var bodyPosition = body.position;

        Profiler.EndSample();
        Profiler.BeginSample("Understeer");

        // poor mans projection
        var A = new Vector2(trackCenter.x, trackCenter.z);
        var B = new Vector2(trackCenter.x + trackForward.x, trackCenter.z + trackForward.z);
        var M = new Vector2(bodyPosition.x, bodyPosition.z);

        lateralError = (M - A).magnitude;

        var side = Mathf.Sign((B.x - A.x) * (M.y - A.y) - (B.y - A.y) * (M.x - A.x));

        if(side != Mathf.Sign(curvature))
        {
            understeer = 0;
            oversteer = lateralError;
        }
        else
        {
            oversteer = 0;
            understeer = lateralError;
        }

        Profiler.EndSample();
        Profiler.BeginSample("Sideslip Angle");

        sideslipAngle = 0;
        foreach (var item in vehicle.wheels)
        {
            if(item.inContact)
            {
                sideslipAngle += item.sideslipAngle;
            }
        }

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

        var forward = trackForward;
        forward.y = 0;
        forward.Normalize();

        var projectedVehicle = new Vector3(vehicle.transform.forward.x, 0f, vehicle.transform.forward.z).normalized;
        var projectedTrack = new Vector3(trackForward.x, 0f, trackForward.z).normalized;
        
        directionError = (1 - Vector3.Dot(projectedTrack, projectedVehicle)) * Mathf.Sign(Vector3.Dot(Vector3.Cross(projectedTrack, projectedVehicle), Vector3.up)); 
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
        if (collision.gameObject.layer == trackLayerMask)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (Mathf.Abs(Vector3.Dot(collision.GetContact(i).normal, Vector3.up)) < 0.5f)
                {
                    collisionOccurred = true;
                }
            }
        }
    }
}
