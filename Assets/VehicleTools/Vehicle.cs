using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Vehicle : MonoBehaviour
{
    public float maxTorque;

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
        float angle = 45 * Input.GetAxis("Horizontal");

        foreach (var wheel in wheels)
        {
            if(wheel.steers)
            {
                wheel.steerAngle = angle;
            }

            wheel.UpdateTransforms();
        }

        foreach (var wheel in wheels)
        {
            wheel.UpdateSuspensionForce();
        }

        int wheelsInContact = 0;

        foreach (var wheel in wheels)
        {
            if(wheel.inContact)
            {
                wheelsInContact++;
            }
        }

        float torque = maxTorque * Input.GetAxis("Vertical");

        foreach (var wheel in wheels)
        {
            wheel.UpdateVelocity();
        }

        foreach (var wheel in wheels)
        {
            if(wheel.inContact)
            {
                wheel.UpdateDriveForce(torque);
                wheel.UpdateGripForce(wheelsInContact);
            }
        }

        foreach(var wheel in wheels)
        {
            wheel.UpdateLocalTransform();
        }
    }
}
