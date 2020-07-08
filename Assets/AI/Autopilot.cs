using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

[RequireComponent(typeof(Navigator))]
public class Autopilot : MonoBehaviour
{
    public float speed;

    /// <summary>
    /// The distance ahead to aim for when steering. The smaller this is, the closer to the turn the car will respond, and the tighter it will attempt to corner.
    /// Increase this value to reduce oversteer.
    /// </summary>
    public float maxLookahead = 50f;

    private Vehicle vehicle;
    private Navigator navigator;

    [NonSerialized]
    public Vector3 worldtarget;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        navigator = GetComponent<Navigator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var maxc = 0f;
        for (int i = 0; i < maxLookahead; i++)
        {
            var c = navigator.waypoints.Query(navigator.PathDistance + i).Curvature;
            maxc = Mathf.Max(Mathf.Abs(c), maxc);
        }
        var h = 1f; // clearance
        var minbase = Mathf.Sqrt(((8f * h) / maxc) - (4f*h*h));

        var lookahead = Mathf.Min(minbase, maxLookahead);

        worldtarget = navigator.waypoints.Query(navigator.PathDistance + lookahead).Midpoint;

        var targetpoint = transform.InverseTransformPoint(worldtarget);
        targetpoint.y = 0;
        targetpoint.Normalize();
        var angle = Mathf.Atan2(targetpoint.x, targetpoint.z);

        var extrema = vehicle.maxSteerAngle * Mathf.Deg2Rad;

        vehicle.steeringAngle = Mathf.Clamp(angle, -extrema, extrema) / extrema;

        var u = speed - vehicle.speed;

        if (u >= 0)
        {
            vehicle.throttle = 1f;
            vehicle.brake = 0;
        }
        else
        {
            vehicle.throttle = 0;
            vehicle.brake = 1f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(worldtarget, 1f);
    }
}
