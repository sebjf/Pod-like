using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

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

    /// <summary>
    /// Vehicle's forward speed in m/s
    /// </summary>
    public float speed;

    private Drivetrain drivetrain;

    [HideInInspector]
    public int wheelsInContact;

    [HideInInspector]
    public float sideslipAngle;

    [HideInInspector]
    public float rpm;

    private const float Rad2Rpm = 9.5493f;

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
        Profiler.BeginSample("Update Wheel Transforms");

        foreach (var wheel in wheels)
        {
            if(wheel.steers)
            {
                wheel.steerAngle = maxSteerAngle * steeringAngle;
            }

            wheel.UpdateTransforms();
        }

        Profiler.EndSample();
        Profiler.BeginSample("Update Suspension Force");

        foreach (var wheel in wheels)
        {
            wheel.UpdateSuspensionForce();
        }

        Profiler.EndSample();

        wheelsInContact = 0;
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

        Profiler.BeginSample("Update Torque");

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

        rpm = Mathf.Abs(angularVelocity * Rad2Rpm);

        var torque = drivetrain.EvaluateTorque(rpm);

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

        Profiler.EndSample();
        Profiler.BeginSample("Update Grip Forces");

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

        Profiler.EndSample();
        Profiler.BeginSample("Update Transforms");

        foreach (var wheel in wheels)
        {
            wheel.UpdateLocalTransform();
        }

        Profiler.EndSample();

        speed = Vector3.Dot(transform.forward, rigidbody.velocity);

        sideslipAngle = 0f;
        foreach (var wheel in wheels)
        {
            if(wheel.inContact)
            {
                sideslipAngle += wheel.sideslipAngle;
            }
        }        
    }
}
