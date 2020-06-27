using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Navigator))]
public class ResetController : MonoBehaviour
{
    [HideInInspector]
    [NonSerialized]
    public Navigator navigator;

    [HideInInspector]
    [NonSerialized]
    public Rigidbody body;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
    }

    public void ResetPosition()
    {
        if (navigator == null)
        {
            navigator = GetComponent<Navigator>();
        }

        var Q = navigator.waypoints.Query(navigator.StartingPosition);

        transform.position = Q.Midpoint + (Vector3.up * 2);
        transform.LookAt(Q.Midpoint + Q.Forward + (Vector3.up * 2), Q.Up);

        navigator.Reset();

        if(body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;

        // the velocities above will be overridden on the next fixedupdate. putting the body to sleep resets the accelerations and forces them to take effect.
        body.Sleep();
    }
}
