using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ResetController
{
    public static void ResetPosition(Navigator navigator)
    {
        var Q = navigator.waypoints.Query(navigator.StartingPosition);

        navigator.transform.position = Q.Midpoint + (Vector3.up * 2);
        navigator.transform.LookAt(Q.Midpoint + Q.Forward + (Vector3.up * 2), Q.Up);

        navigator.Reset();

        var body = navigator.GetComponent<Rigidbody>();
        if(body)
        {
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            // the velocities above will be overridden on the next fixedupdate. putting the body to sleep resets the accelerations and forces them to take effect.
            body.Sleep();
        }
    }

    public static void PlacePrefab(Transform car, TrackPath geometry, float distance, float offset)
    {
        var q = geometry.Query(distance);
        car.position = q.Midpoint + (q.Tangent * offset) + car.localPosition;
        car.forward = q.Forward;
        car.up = q.Up;
    }

}
