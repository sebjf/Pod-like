using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Navigator))]
public class Autopilot : MonoBehaviour
{
    public float speed;
    public float offset;

    private Vehicle vehicle;
    private Rigidbody body;
    private Navigator navigator;

    private Vector3 worldtarget;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        worldtarget = navigator.waypoints.Query(navigator.TrackDistance + 10).Midpoint;

        var targetpoint = transform.InverseTransformPoint(worldtarget);
        targetpoint.y = 0;
        targetpoint.Normalize();
        var angle = Mathf.Atan2(targetpoint.x, targetpoint.z);

        var extrema = vehicle.maxSteerAngle * Mathf.Deg2Rad;

        vehicle.steeringAngle = Mathf.Clamp(angle, -extrema, extrema) / extrema;

        var u = (speed - vehicle.speed) + offset;

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
