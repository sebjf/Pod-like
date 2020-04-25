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

    private Rigidbody body;
    private Navigator navigator;
    private Autopilot autopilot;

    [HideInInspector]
    public float[] profile;

    [HideInInspector]
    public float[] curvature;
    [HideInInspector]
    public float[] camber;
    [HideInInspector]
    public float[] inclination;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        navigator = GetComponent<Navigator>();
        autopilot = GetComponent<Autopilot>();
        workers = new List<IWorker>();
 //       for (int i = 0; i < 10; i++)
        {
            var model = ModelLoader.LoadFromStreamingAssets(modelName + ".nn");
            workers.Add(WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model));
            inputs = new Tensor(new TensorShape(model.inputs[0].shape));
            numObservations = (model.inputs[0].shape.Last() - 1) / 3;
        }
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

        List<float> targetspeeds = new List<float>();

        for (int i = 0; i < numObservations; i++)
        {
            var distance = navigator.TrackDistance + i * observationsInterval;
            var Q = navigator.waypoints.Query(distance);
            curvature[i] = Q.Curvature;
            camber[i] = Q.Camber;
            inclination[i] = Q.Inclination;
        }

        // pack the inputs F
        for (int i = 0; i < numObservations; i++)
        {
            inputs[(i * 3) + 0 + 1] = curvature[i] * 20f;
            inputs[(i * 3) + 1 + 1] = camber[i] * 200f;
            inputs[(i * 3) + 2 + 1] = inclination[i] * 3f;
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

        foreach (var worker in workers)
        {
            worker.Execute(inputs);

            var output = worker.PeekOutput();
            for (int i = 0; i < output.length; i++)
            {
                profile[i] = output[i];
            }

            targetspeeds.Add(profile[1]);
        }

        //autopilot.speed = profile[1];
        autopilot.speed = targetspeeds.Max();
    }
}
