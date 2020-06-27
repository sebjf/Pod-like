using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Navigator))]
[RequireComponent(typeof(Autopilot))]
[RequireComponent(typeof(Vehicle))]
public class ProfileController : MonoBehaviour
{
    public string modelName;
    public int observationsInterval = 10;

    public float x = 20f;

    private List<IWorker> workers;
    private IWorker classificationWorker;
    private Tensor inputs;
    private int numObservations;

    private Rigidbody body;
    private Navigator navigator;
    private Autopilot autopilot;
    private Vehicle vehicle;

    private List<float> estimations;

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
        vehicle = GetComponent<Vehicle>();
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

        var sideslip = vehicle.sideslipAngle; // can be up to 90 (per wheel), but in practice force will top out around 5 deg.

        inputs[1] = sideslip * 0.01f;  

        for (int i = 0; i < numObservations; i++)
        {
            var Q = navigator.waypoints.Query(navigator.PathDistance + i * observationsInterval);
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

        bool slipping = false;

        var a = (Mathf.Rad2Deg * sideslip) / x;

        autopilot.speed = min + (1 - a) * (max - min); //(the ability for (1-a) to be negative here is deliberate...)

        if (a >= 1)
        {
            slipping = true;
        }

        if (!slipping)
        {
            classificationWorker.Execute(inputs);
            var classification = classificationWorker.PeekOutput()[0];
            var isstraight = classification < 0.5f;

            if (isstraight)
            {
                autopilot.speed = 100f;
            }
        }
    }
}
