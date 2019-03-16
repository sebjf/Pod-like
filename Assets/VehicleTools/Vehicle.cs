using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Vehicle : MonoBehaviour
{
    private Wheel[] wheels;
    private Drivetrain drivetrain;

    public float steerAngle;

    private void Awake()
    {
        wheels = GetComponentsInChildren<Wheel>();
        drivetrain = GetComponent<Drivetrain>();
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
        float angle = steerAngle * Input.GetAxis("Horizontal");

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

        var angularVelocity = 0f;
        var angularVelocityCount = 0f;
        foreach (var wheel in wheels)
        {
            if (wheel.drives)
            {
                angularVelocity += wheel.angularVelocity; angularVelocityCount++;
            }
        }

        angularVelocity /= angularVelocityCount;

        var torque = drivetrain.EvaluateTorque(angularVelocity);

        foreach (var wheel in wheels)
        {
            if (wheel.drives)
            {
                wheel.ApplyDriveTorque(torque);
            }
        }

        foreach (var wheel in wheels)
        {
            wheel.UpdateVelocity();
        }

        foreach (var wheel in wheels)
        {
            if (wheel.inContact)
            {
                wheel.UpdateDriveForce();
                wheel.UpdateGripForce(wheelsInContact);
            }
        }

        foreach(var wheel in wheels)
        {
            wheel.UpdateLocalTransform();
        }
    }
}
