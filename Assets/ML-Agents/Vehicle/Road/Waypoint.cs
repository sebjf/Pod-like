using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public float width;
    public Waypoint next;

    public Vector3 nextposition
    {
        get
        {
            if (next != null)
            {
                return next.transform.position;
            }
            else
            {
                return transform.position + transform.forward;
            }
        }
    }

    public Vector3 normal
    {
        get
        {
            return (nextposition - transform.position).normalized;
        }
    }

    public Vector3 tangent
    {
        get
        {
            return Vector3.Cross(normal, Vector3.up);
        }
    }



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
        Gizmos.DrawLine(transform.position, nextposition);
        Gizmos.DrawLine(transform.position, transform.position + tangent * width);
        Gizmos.DrawLine(transform.position, transform.position - tangent * width);
    }
}
