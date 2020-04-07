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
    
    private IWorker worker;
    private Model model;
    private Tensor inputs;
    private int numObservations;

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
        model = ModelLoader.LoadFromStreamingAssets(modelName + ".nn");
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        inputs = new Tensor(new TensorShape(model.inputs[0].shape));
        numObservations = (model.inputs[0].shape.Last() - 1) / 3;
        profile = new float[numObservations];
        curvature = new float[numObservations];
        camber = new float[numObservations];
        inclination = new float[numObservations];
    }

    private void OnDestroy()
    {
        if (worker != null)
        {
            worker.Dispose();
            worker = null;
        }
        if(inputs != null)
        {
            inputs.Dispose();
            inputs = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        UpdateObservations();
        worker.Execute(inputs);
        var output = worker.PeekOutput();
        for (int i = 0; i < output.length; i++)
        {
            profile[i] = output[i];
        }
        autopilot.speed = profile[1];
    }

    void UpdateObservations()
    {
        // the following magic numbers are built into the network

        inputs[0] = body.velocity.magnitude;

        for (int i = 0; i < numObservations; i++)
        {
            var distance = navigator.TrackDistance + i * 10f;
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

    }
}
