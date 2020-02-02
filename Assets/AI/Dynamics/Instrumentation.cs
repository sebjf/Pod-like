using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Instrumentation : MonoBehaviour
{
    public string logfile;

    private Vehicle vehicle;

    public struct Frame
    {
        public float oversteer;
        public float steeringangle;
        public float speed;
        public float turningangle;

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", speed, turningangle, oversteer, steeringangle);
        }
    }

    private List<Frame> log;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
        log = new List<Frame>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // driver assist

        var VR = 0f;
        var VF = 0f;
        foreach (var wheel in vehicle.wheels)
        {
            if (wheel.localAttachmentPosition.z > vehicle.GetComponent<Rigidbody>().centerOfMass.z)
            {
                VF += Vector3.Dot(wheel.velocity, vehicle.GetComponent<Rigidbody>().transform.right);
            }
            if (wheel.localAttachmentPosition.z < vehicle.GetComponent<Rigidbody>().centerOfMass.z)
            {
                VR += Vector3.Dot(wheel.velocity, vehicle.GetComponent<Rigidbody>().transform.right);
            }
        }

        //   if (Mathf.Abs(VR) > Mathf.Abs(VF))
        {
            FindObjectOfType<GraphOverlay>().SetLabel("DriftingLabel", (Mathf.Abs(VR) - Mathf.Abs(VF)).ToString());
        }

        Frame frame;
        frame.oversteer = Mathf.Abs(VR) - Mathf.Abs(VF);
        frame.steeringangle = vehicle.steeringAngle;
        frame.speed = vehicle.GetComponent<Rigidbody>().velocity.magnitude;
        frame.turningangle = vehicle.GetComponent<Rigidbody>().angularVelocity.y;

        log.Add(frame);
    }

    private void OnDestroy()
    {
        if (enabled)
        {
            var csv = "";
            foreach (var item in log)
            {
                csv += item.ToString() + "\n";
            }
            System.IO.File.WriteAllText(logfile, csv);
        }
    }
}
