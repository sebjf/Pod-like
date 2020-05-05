using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drivetrain : MonoBehaviour
{
    [Range(0,1)]
    public float throttle;

    public AnimationCurve torqueCurve;
    public float torqueCurveScalar = 10000;
    public float rpmScalar = 2000;

    private const float Rad2Rpm = 9.5493f;

    public float EvaluateTorque(float wheelAngularVelocity)
    {
        var rpm = wheelAngularVelocity * Rad2Rpm;
        var torque = torqueCurve.Evaluate(Mathf.Abs(rpm / rpmScalar)) * torqueCurveScalar * throttle;
        return torque;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
}
