using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Navigator))]
public class ResetController : MonoBehaviour
{
    [HideInInspector]
    [NonSerialized]
    public Navigator navigator;

    protected Rigidbody body;

    [HideInInspector]
    [NonSerialized]
    public float forwardvariation = 0; // in m

    public void ResetPosition(float startPosition)
    {
        navigator.StartingPosition = startPosition;
        ResetPosition();
    }

    public void ResetPosition()
    {
        if (navigator == null)
        {
            navigator = GetComponent<Navigator>();
            navigator.Reset();
        }

        var trackposition = navigator.StartingPosition + UnityEngine.Random.Range(-forwardvariation, forwardvariation);
        var Q = navigator.waypoints.Query(trackposition);

        transform.position = Q.Midpoint + (Vector3.up * 2);
        transform.forward = Q.Forward;

        navigator.Reset();

        if(body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        body.velocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }
}
