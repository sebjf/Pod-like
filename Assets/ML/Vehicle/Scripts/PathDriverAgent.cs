using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Barracuda;
using System;

[Serializable]
public struct StabilityFunction
{
    public float x;

    public float Evaluate(float sideslip)
    {
        if ((Mathf.Rad2Deg * sideslip) > x)
        {
            return 0f;
        }
        else
        {
            return 1f;
        }
    }
}


[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Navigator))]
[RequireComponent(typeof(Autopilot))]
public class PathDriverAgent : MonoBehaviour
{
    public string modelName;
    public int observationsInterval = 10;

    private List<IWorker> workers;
    private IWorker classificationWorker;
    private Tensor inputs;
    private int numObservations;

    private Rigidbody body;
    private Navigator navigator;
    private Autopilot autopilot;
    private Wheel[] wheels;

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
    public float sideslip;

    public StabilityFunction stabilityFunction;

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

        classificationWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, ModelLoader.LoadFromStreamingAssets(modelName + "C" + ".nn"));

        numObservations = (inputShape.Last() - 1) / 3;
        inputs = new Tensor(new TensorShape(inputShape));
        estimations = new List<float>();

        profile = new float[numObservations];
        curvature = new float[numObservations];
        camber = new float[numObservations];
        inclination = new float[numObservations];
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

        sideslip = 0f;
        foreach (var item in wheels)
        {
            if (item.inContact)
            {
                sideslip += item.sideslipAngle; // can be up to 90, but in practice force will top out around 5 deg.
            }
        }
        inputs[1] = sideslip * 0.01f;

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
        var a = stabilityFunction.Evaluate(sideslip);

        autopilot.speed = (max * a) + (min * (1 - a));


        classificationWorker.Execute(inputs);
        var classification = classificationWorker.PeekOutput()[0];
        var isstraight = classification < 0.5f;

        if (isstraight)
        {
            autopilot.speed = 100f;
        }
    }
}
