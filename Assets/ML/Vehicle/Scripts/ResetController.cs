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
    public float startPosition = -1;

    [HideInInspector]
    [NonSerialized]
    public float forwardvariation = 0; // in m
    [HideInInspector]
    [NonSerialized]
    public float lateralvariation = 0; // normalised width

    public void ResetPosition(float startPosition)
    {
        this.startPosition = startPosition;
        ResetPosition();
    }

    public void ResetPosition()
    {
        if (navigator == null)
        {
            navigator = GetComponent<Navigator>();
            navigator.Reset();
        }

        if (startPosition < 0)
        {
            startPosition = navigator.TrackDistance;
        }

        var trackposition = startPosition + UnityEngine.Random.Range(-forwardvariation, forwardvariation);
        var Q = navigator.waypoints.Query(trackposition);

        transform.position = Q.Midpoint + (Vector3.up * 2) + (Q.Tangent * UnityEngine.Random.Range(-lateralvariation, lateralvariation));
        transform.forward = navigator.waypoints.Normal(trackposition);

        navigator.Reset();

        if(body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        body.velocity = Vector3.zero;
    }
}
