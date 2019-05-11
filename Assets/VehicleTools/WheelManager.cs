using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WheelSettings
{
    public AnimationCurve slipForce;
    public float slipForceScale;
    public AnimationCurve forwardSlipForce;
    public float forwardSlipForceScale;
    public float brakingTorque;

    public void Reset(WheelSettings other)
    {
        slipForce = new AnimationCurve(other.slipForce.keys);
        slipForceScale = other.slipForceScale;
        forwardSlipForce = new AnimationCurve(other.forwardSlipForce.keys);
        forwardSlipForceScale = other.forwardSlipForceScale;
        brakingTorque = other.brakingTorque;
    }

    public void Reset(Wheel wheel)
    {
        slipForce = new AnimationCurve(wheel.slipForce.keys);
        slipForceScale = wheel.slipForceScale;
        forwardSlipForce = new AnimationCurve(wheel.forwardSlipForce.keys);
        forwardSlipForceScale = wheel.forwardSlipScale;
        brakingTorque = wheel.brakingTorque;
    }

    public void Update(Wheel wheel)
    {
        wheel.slipForce = slipForce;
        wheel.slipForceScale = slipForceScale;
        wheel.forwardSlipForce = forwardSlipForce;
        wheel.forwardSlipScale = forwardSlipForceScale;
        wheel.brakingTorque = brakingTorque;
    }
}

// Helper component to override a subset of wheel parameters
public class WheelManager : MonoBehaviour
{
    [Serializable]
    public class WheelAssignment
    {
        public WheelSettings settings;
        public List<Wheel> wheels = new List<Wheel>();
    }

    public List<WheelAssignment> wheelAssignments;

    public void Reset()
    {
        var allwheels = GetComponentsInChildren<Wheel>().ToList();
        var frontWheelSettings = new WheelSettings();
        frontWheelSettings.Reset(allwheels.First());

        var wheelassignment = new WheelAssignment();
        wheelassignment.settings = frontWheelSettings;
        wheelassignment.wheels.AddRange(allwheels);

        wheelAssignments = new List<WheelAssignment>();
        wheelAssignments.Add(wheelassignment);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var assignment in wheelAssignments)
        {
            foreach (var wheel in assignment.wheels)
            {
                assignment.settings.Update(wheel);
            }
        }
    }
}
