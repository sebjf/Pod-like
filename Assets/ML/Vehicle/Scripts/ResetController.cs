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

    [HideInInspector]
    [NonSerialized]
    public Rigidbody body;

    [HideInInspector]
    [NonSerialized]
    public float forwardvariation = 0; // in m

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
    }

    public void ResetPosition(float startPosition)
    {
        if (navigator == null)
        {
            navigator = GetComponent<Navigator>();
        }

        navigator.StartingPosition = startPosition;

        ResetPosition();
    }

    public void ResetPosition()
    {
        if (navigator == null)
        {
            navigator = GetComponent<Navigator>();
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
