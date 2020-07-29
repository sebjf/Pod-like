using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class Drivetrain : MonoBehaviour
{
    [Range(0,1)]
    public float throttle;

    public AnimationCurve torqueCurve;

    private float maxRpm;

    private void Awake()
    {
        maxRpm = torqueCurve.keys.Max(k => k.time);
    }

    public float EvaluateTorque(float rpm)
    {
        var torque = torqueCurve.Evaluate(rpm) * throttle;
        return torque;
    }

    public float EvaluateEngineRange(float rpm)
    {
        return Mathf.Abs(rpm) / maxRpm;
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
