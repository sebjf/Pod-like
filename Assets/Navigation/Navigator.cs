using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class Navigator : MonoBehaviour
{
    [HideInInspector]
    public Waypoints waypoints;

    [NonSerialized]
    public float distance = -1;

    public void Reset()
    {
        distance = -1f;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if(waypoints == null)
        {
            waypoints = FindObjectOfType<Waypoints>();
        }

        waypoints.InitialiseTemporaryBroadphase();

        distance = waypoints.Evaluate(transform.position, distance);
    }

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
        {
            FixedUpdate();
        }

        var midline = waypoints.Midline(distance);

        Gizmos.DrawLine(midline, transform.position);

        var normal = waypoints.Normal(distance);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(midline, normal);
    }
}