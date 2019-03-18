using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Vehicle : MonoBehaviour
{
    private Drivetrain drivetrain;

    public float maxSteerAngle;

    [HideInInspector]
    public Wheel[] wheels;

    [HideInInspector]
    public float steeringAngle;

    [HideInInspector]
    public new Rigidbody rigidbody;

    public bool brake;
    public bool handbrake;

    private void Awake()
    {
        wheels = GetComponentsInChildren<Wheel>();
        drivetrain = GetComponent<Drivetrain>();
        rigidbody = GetComponent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        drivetrain.throttle = Input.GetAxis("Vertical");
        brake = Input.GetKey(KeyCode.X);
    }

    private void FixedUpdate()
    {
        steeringAngle = maxSteerAngle * Input.GetAxisRaw("Horizontal");

        foreach (var wheel in wheels)
        {
            if(wheel.steers)
            {
                wheel.steerAngle = steeringAngle;
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
            if (brake || handbrake)
            {
                wheel.ApplyBrake(1);
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
    }
}
