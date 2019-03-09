using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public float maxTorque = 1000;
    public float lateralForce = 10;

    // Update is called once per frame
    void Update()
    {
        foreach (var item in GetComponentsInChildren<Wheel>())
        {
            item.maxTorque = maxTorque;
            item.lateralForce = lateralForce;
        }
    }
}
