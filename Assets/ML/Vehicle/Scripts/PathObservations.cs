using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathObservations : MonoBehaviour
{
    private Navigator navigator;
    private Rigidbody body;
    private Vehicle vehicle;

    public GraphOverlay graph;

    [HideInInspector]
    public float speed;

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

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
        vehicle = GetComponent<Vehicle>();
    }

    void FixedUpdate()
    {
        var trackCenter = navigator.waypoints.Evaluate(navigator.TrackDistance);
        var bodyPosition = body.position;
        var trackForward = navigator.waypoints.Normal(navigator.TrackDistance);
        var curvature = navigator.waypoints.Curvature(navigator.TrackDistance);

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

        height = 0;
        if (vehicle.wheelsInContact < 2)
        {
            RaycastHit raycast;
            if (Physics.Raycast(new Ray(body.position, Vector3.down), out raycast, float.PositiveInfinity, LayerMask.GetMask("Track")))
            {
                height = raycast.distance;
            }
        }

        traction = vehicle.wheelsInContact > 2;
        speed = body.velocity.magnitude;

        if (graph) graph.GetSeries("lateralError", Color.red).values.Add(-lateralError);
    }
}
