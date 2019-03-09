using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Vehicle : MonoBehaviour
{
    private Wheel[] wheels;

    private void Awake()
    {
        wheels = GetComponentsInChildren<Wheel>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        int wheelsInContact = 0;

        foreach (var wheel in wheels)
        {
            wheel.UpdateSuspensionForce();
        }

        foreach (var wheel in wheels)
        {
            if(wheel.incontact)
            {
                wheelsInContact++;
            }
        }

        foreach (var wheel in wheels)
        {
            if(wheel.incontact)
            {
                wheel.UpdateDriveForce();
                wheel.UpdateGripForce(wheelsInContact);
            }
        }
    }
}
