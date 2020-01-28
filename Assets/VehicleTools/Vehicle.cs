using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Vehicle : MonoBehaviour
{
    [Range(-1,1)]
    public float throttle;

    [Range(-1,1)]
    public float steeringAngle;

    [Range(0,1)]
    public float brake;

    public bool handbrake;

    public float maxSteerAngle = 40f;

    [HideInInspector]
    public Wheel[] wheels;  

    [HideInInspector]
    public new Rigidbody rigidbody;

    public float speed;

    private Drivetrain drivetrain;

    public void Awake()
    {
        wheels = GetComponentsInChildren<Wheel>();
        drivetrain = GetComponent<Drivetrain>();
        rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    public void Update()
    {
        drivetrain.throttle = throttle;
    }

    public void FixedUpdate()
    {
        foreach (var wheel in wheels)
        {
            if(wheel.steers)
            {
                wheel.steerAngle = maxSteerAngle * steeringAngle;
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

        foreach (var wheel in wheels)
        {
            wheel.wheelsInContact = wheelsInContact;
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
            var brakepower = brake;
            if(handbrake)
            {
                brakepower = 1f;
            }
            if(brakepower > 0f)
            {
                wheel.ApplyBrake(brakepower);
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
                wheel.UpdateGripForce();
            }
        }

        foreach (var wheel in wheels)
        {
            wheel.UpdateLocalTransform();
        }

        speed = Vector3.Dot(transform.forward, rigidbody.velocity);
    }
}
