using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrainingWorker : MonoBehaviour
{
    private TrainingManager trainingManager;

    private List<float> interpolationCoefficients;

    public event Action<IAgentManager> OnTrainingRequestComplete;
    public event Action<TrainingRequest, IAgentManager> OnTrainingFrame;

    private void Awake()
    {
        trainingManager = GetComponent<TrainingManager>();

        interpolationCoefficients = new List<float>();
        interpolationCoefficients.Add(0.5f);
        interpolationCoefficients.Add(0.75f);
        interpolationCoefficients.Add(0.25f);
    }

    private void Start()
    {
        StartCoroutine(TrainingWorkerCoroutine());
    }

    public IEnumerator TrainingWorkerCoroutine()
    {
        while (true)
        {
            TrainingRequest request;
            while (!trainingManager.requests.TryDequeue(out request))
            {
                yield return null;
            }

            // dereference to the actual objects that will be passed around
            var path = trainingManager.Circuit(request.circuit);
            var prefab = trainingManager.Car(request.car);

            // load the scene and create the agent trainer
            var asyncoperation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);

            while (!asyncoperation.isDone)
            {
                yield return null;
            }

            var scene = SceneManager.GetSceneByName(path);
            var root = scene.GetRootGameObjects().Select(x => x.GetComponentInChildren<Track>()).Where(x => x != null).First();

            // set up interpolated paths. this should be done before the manager is added. 
            var geometry = root.GetComponentInChildren<TrackGeometry>();

            // delete existing interpolations
            var existing = geometry.GetComponentsInChildren<InterpolatedPath>();
            foreach (var item in existing)
            {
                DestroyImmediate(item); // delete immediately as the manager should search for paths in its Start() method.
            }

            // and cars
            var cars = geometry.transform.Find("Cars");
            if(cars)
            {
                Destroy(cars.gameObject);
            }

            foreach (var item in interpolationCoefficients)
            {
                var interpolated = geometry.gameObject.AddComponent<InterpolatedPath>();
                interpolated.coefficient = item;
                interpolated.Initialise();
            }

            // we add the manager right away. we don't need to worry about removing the old one, because the scene will be unloaded at the end of this

            IAgentManager manager = null;
            if(request.agent == "profile")
            {
                manager = root.gameObject.AddComponent<ProfileAgentManager>();
            }
            if(request.agent == "path")
            {
                manager = root.gameObject.AddComponent<PathAgentManager>();
            }
            
            manager.SetAgentPrefab(prefab);

            var training = true;
            var running = true;

            // setup the callbacks
            manager.OnComplete.RemoveAllListeners();
            manager.OnComplete.AddListener((x) =>
            {
                Debug.Log("TrainingRequest Complete");
                OnTrainingRequestComplete?.Invoke(manager);
                request.circuit = "";
                request.car = "";
                OnTrainingFrame?.Invoke(request, manager);
                training = false;
                SceneManager.UnloadSceneAsync(path).completed += (asyncresult) =>
                {
                    running = false;
                };
            });

            // and let it go...

            while (running)
            {
                if (training)
                {
                    OnTrainingFrame?.Invoke(request, manager);
                }
                yield return null;
            }
        }
    }
}
