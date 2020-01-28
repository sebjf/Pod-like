using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DynamicsProfiler : MonoBehaviour
{
    private Vehicle vehicle;

    public string filename;

    private void Reset()
    {
        // create asset here...
    }

    public enum Mode
    {
        Acceleration,
        Deceleration
    }

    public Mode mode;

    public struct Sample
    {
        public float time;
        public float speed;
    }

    public List<Sample> samples;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();

        var vehicleControllerInput = vehicle.GetComponent<VehicleControllerInput>();
        if(vehicleControllerInput != null)
        {
            vehicleControllerInput.enabled = false;
        }

        var autopilot = vehicle.GetComponent<Autopilot>();
        if (autopilot != null)
        {
            autopilot.enabled = false;
        }

        samples = new List<Sample>();
    }

    // Start is called before the first frame update
    void Start()
    {
        switch (mode)
        {
            case Mode.Acceleration:
                vehicle.throttle = 1f;
                vehicle.brake = 0f;
                break;
            case Mode.Deceleration:
                vehicle.throttle = 0f;
                vehicle.brake = 1f;
                vehicle.rigidbody.velocity = vehicle.transform.forward * 150f;
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        var sample = new Sample();
        sample.time = Time.time;
        sample.speed = vehicle.speed;
        samples.Add(sample);
    }

    private void OnDestroy()
    {
        if(filename != null)
        {
            if(filename != "")
            {
                using(StreamWriter writer = new StreamWriter(filename))
                {
                    foreach (var item in samples)
                    {
                        writer.WriteLine(string.Format("{0},{1}", item.time, item.speed));
                    }
                }
            }
        }
    }

    public void SampleProfile(List<Sample> samples, AccelerationProfile profile)
    {
        float[] dSpeed = new float[samples.Count - 1];
        for (int i = 0; i < dSpeed.Length - 1; i++)
        {
            dSpeed[0] = samples[i + 1].speed - samples[i].speed;
        }


    }

    public float[] Diff(float[] X)
    {
        var Y = new float[X.Length - 1];
        for (int i = 0; i < Y.Length; i++)
        {
            Y[i] = X[i + 1] - X[i];
        }
        return Y;
    }

    /// <summary>
    /// Samples Y indexed by X for positions XS. Assumes that both X and XS are monotonic increasing.
    /// </summary>
    public float[] ResampleOrdered(float[] X, float[] Y, float[] XS)
    {
        var YS = new float[XS.Length];
        var x = 0;
        for (int s = 0; s < XS.Length; s++)
        {
            var xs = XS[s];
            while (true)
            {
                if(x >= X.Length - 1)
                {
                    YS[s] = Y[Y.Length - 1];
                    break;
                }

                if(xs < X[x])
                {
                    YS[s] = Y[0];
                    break;
                }

                if (X[x] >= xs && xs <= X[x + 1])
                {
                    var t = (xs - X[x]) / (X[x + 1] - X[x]);
                    YS[s] = Mathf.Lerp(Y[x], Y[x + 1], t);
                    break;
                }

                x++;
            }
        }
        return YS;
    }
    
}
