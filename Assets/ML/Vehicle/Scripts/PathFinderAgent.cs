using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Barracuda;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Navigator))]
[RequireComponent(typeof(Autopilot))]
public class PathFinderAgent : MonoBehaviour
{
    public string modelName;
    
    private List<IWorker> workers;
    private Tensor inputs;
    private int numObservations;
    private float observationsInterval;

    public float curvatureThreshold;

    private Rigidbody body;
    private Navigator navigator;
    private Autopilot autopilot;

    private List<float> estimations;

    [HideInInspector]
    public float[] profile;

    [HideInInspector]
    public float[] curvature;
    [HideInInspector]
    public float[] camber;
    [HideInInspector]
    public float[] inclination;

    [HideInInspector]
    public float drift;

    private Wheel[] wheels;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        navigator = GetComponent<Navigator>();
        autopilot = GetComponent<Autopilot>();
        wheels = GetComponentsInChildren<Wheel>();
        workers = new List<IWorker>();

        int[] inputShape = null;
        for (int i = 0; i < 5; i++)
        {
            var model = ModelLoader.LoadFromStreamingAssets(modelName + i + ".nn");
            workers.Add(WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model));
            inputShape = model.inputs[0].shape;
        }

        numObservations = (inputShape.Last() - 1) / 3;
        inputs = new Tensor(new TensorShape(inputShape));
        estimations = new List<float>();

        profile = new float[numObservations];
        curvature = new float[numObservations];
        camber = new float[numObservations];
        inclination = new float[numObservations];
        observationsInterval = 10f;
    }

    private void OnDestroy()
    {
        if (workers != null)
        {
            foreach (var item in workers)
            {
                item.Dispose();
            }
            workers = null;
        }
        if(inputs != null)
        {
            inputs.Dispose();
            inputs = null;
        }
    }

    private void FixedUpdate()
    {
        // the following magic numbers are built into the network

        inputs[0] = body.velocity.magnitude;

        drift = 1 - Vector3.Dot(body.transform.forward, navigator.waypoints.Query(navigator.TrackDistance).Forward);

        inputs[1] = drift;

        for (int i = 0; i < numObservations; i++)
        {
            var Q = navigator.waypoints.Query(navigator.TrackDistance + i * observationsInterval);
            curvature[i] = Q.Curvature;
            camber[i] = Q.Camber;
            inclination[i] = Q.Inclination;
        }

        // pack the inputs F
        for (int i = 0; i < numObservations; i++)
        {
            inputs[(i * 3) + 0 + 2] = curvature[i] * 20f;
            inputs[(i * 3) + 1 + 2] = camber[i] * 200f;
            inputs[(i * 3) + 2 + 2] = inclination[i] * 3f;
        }

        // pack inputs C
        /*
        for (int i = 0; i < numObservations; i++)
        {
            inputs[i + numObservations * 0 + 1] = curvature[i] * 20f;
            inputs[i + numObservations * 1 + 1] = camber[i] * 200f;
            inputs[i + numObservations * 2 + 1] = inclination[i] * 3f;
        }
        */

        bool isstraight = true;

        for (int i = 0; i < numObservations; i++)
        {
            if (Mathf.Abs(curvature[i] * 20f) > curvatureThreshold)
            {
                isstraight = false;
            }
        }

        estimations.Clear();
        foreach (var worker in workers)
        {
            worker.Execute(inputs);

            var output = worker.PeekOutput();
            profile[0] = output[0];
            estimations.Add(output[0]);
        }

        var min = estimations.Min();
        var max = estimations.Max();


        //autopilot.speed = min + (max - min) * (1 - (drift + 0.5f));

        var slip = 0f;
        foreach (var item in wheels)
        {
            slip += item.sideslipAngle; // can be up to 90, but in practice force will top out around 5 deg.
        }

        drift = slip;

        if ((Mathf.Rad2Deg * drift) > 15f)
        {
            autopilot.speed = min;
        }
        else
        {
            autopilot.speed = max;
        }

        //autopilot.speed = estimations.Average();

        /*
        var avg = estimations.Average();
        var sum = estimations.Sum(d => Mathf.Pow(d - avg, 2));
        variance = Mathf.Sqrt(sum / (float)(estimations.Count - 1));
        */

        if(isstraight)
        {
            autopilot.speed = 100f;
        }
    }
}
