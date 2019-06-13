using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        var wp = GameObject.FindObjectOfType<Waypoints>();
        var d = wp.Evaluate(transform.position);
        var midline = wp.Midline(d);

        Gizmos.DrawLine(midline, transform.position);

        var normal = wp.Normal(d);

        Gizmos.DrawRay(midline, normal);
    }
}
