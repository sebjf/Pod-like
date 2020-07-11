using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackNavigator : Navigator
{
    public float NoseOffset = 0f;

    private void Awake()
    {
        waypoints = GetComponentInParent<TrackGeometry>();
    }

    public override Vector3 position
    {
        get { return transform.position + transform.forward * NoseOffset; }
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * NoseOffset, 0.1f);
        base.OnDrawGizmosSelected();
    }
}