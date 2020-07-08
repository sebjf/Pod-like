using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drivetrain : MonoBehaviour
{
    [Range(0,1)]
    public float throttle;

    public AnimationCurve torqueCurve;

    public float EvaluateTorque(float rpm)
    {
        var torque = torqueCurve.Evaluate(rpm) * throttle;
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
