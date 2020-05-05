using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
public class PathObservations : MonoBehaviour
{
    private Navigator navigator;
    private Rigidbody body;
    private Vehicle vehicle;

    [HideInInspector]
    public float speed;

    [HideInInspector]
    public float drift;

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

    private int layerMask;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
        vehicle = GetComponent<Vehicle>();
        layerMask = LayerMask.GetMask("Track");
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
        Profiler.BeginSample("Drift");

        drift = 1 - Vector3.Dot(body.transform.forward, trackForward);

        Profiler.EndSample();
        Profiler.BeginSample("Height");

        height = 0;
        if (vehicle.wheelsInContact < 2)
        {
            RaycastHit raycast;
            if (Physics.Raycast(new Ray(body.position, Vector3.down), out raycast, float.PositiveInfinity, layerMask))
            {
                height = raycast.distance;
            }
        }

        Profiler.EndSample();

        traction = vehicle.wheelsInContact > 2;
        speed = body.velocity.magnitude;
    }
}
