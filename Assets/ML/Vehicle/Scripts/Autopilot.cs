using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Navigator))]
public class Autopilot : MonoBehaviour
{
    public float target;
    public float speed;

    private Vehicle vehicle;
    private Navigator navigator;

    private Vector3 worldtarget;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        navigator = GetComponent<Navigator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        worldtarget = navigator.waypoints.Evaluate(navigator.TrackDistance + 10, target);

        var targetpoint = transform.InverseTransformPoint(worldtarget);
        targetpoint.y = 0;
        targetpoint.Normalize();
        var angle = Mathf.Atan2(targetpoint.x, targetpoint.z);

        var extrema = vehicle.maxSteerAngle * Mathf.Deg2Rad;

        vehicle.steeringAngle = Mathf.Clamp(angle, -extrema, extrema) / extrema;

        var u = (speed - vehicle.speed);

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
