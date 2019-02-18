using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MeasureTool : MonoBehaviour
{
    public Transform from;
    public Transform to;

    public float distance;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(from)
        {
            if(to)
            {
                distance = (from.position - to.position).magnitude;
            }
        }
    }
}
