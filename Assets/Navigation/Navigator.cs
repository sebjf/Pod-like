using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[ExecuteInEditMode]
public class Navigator : MonoBehaviour
{
    private Waypoints wp;

    [NonSerialized]
    public float distance = -1;

    public void Reset()
    {
        distance = -1f;
    }

    // Update is called once per frame
    public void FixedUpdate()
    {
        if(wp == null)
        {
            wp = FindObjectOfType<Waypoints>();
        }

        wp.InitialiseTemporaryBroadphase();

        distance = wp.Evaluate(transform.position, distance);
    }

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying)
        {
            FixedUpdate();
        }

        var midline = wp.Midline(distance);

        Gizmos.DrawLine(midline, transform.position);

        var normal = wp.Normal(distance);

        Gizmos.DrawRay(midline, normal);
    }
}
