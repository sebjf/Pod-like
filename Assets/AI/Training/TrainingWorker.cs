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

    public event Action<ProfileAgentManager> OnTrainingRequestComplete;
    public event Action<TrainingRequest, ProfileAgentManager> OnTrainingFrame;

    private void Awake()
    {
        trainingManager = GetComponent<TrainingManager>();

        interpolationCoefficients = new List<float>();
        interpolationCoefficients.Add(0.5f);
        interpolationCoefficients.Add(0.15f);
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

            var scene = SceneManager.GetSceneByPath(path);
            var root = scene.GetRootGameObjects().Select(x => x.GetComponentInChildren<Track>()).Where(x => x != null).First();

            var manager = scene.GetRootGameObjects().Select(x => x.GetComponentInChildren<ProfileAgentManager>()).Where(x => x != null).FirstOrDefault();
            if (manager == null)
            {
                manager = root.gameObject.AddComponent<ProfileAgentManager>();
                // set up manager further here...
            }
            manager.AgentPrefab = prefab;

            // set up interpolated paths...
            var geometry = root.GetComponentInChildren<TrackGeometry>();
            var existing = geometry.GetComponentsInChildren<InterpolatedPath>().Select(x => x.coefficient).ToList();
            foreach (var item in interpolationCoefficients)
            {
                if (existing.Contains(item))
                {
                    continue;
                }
                var interpolated = geometry.gameObject.AddComponent<InterpolatedPath>();
                interpolated.coefficient = item;
                interpolated.Initialise();
            }

            var training = true;

            // setup the callbacks
            manager.OnComplete.RemoveAllListeners();
            manager.OnComplete.AddListener((x) =>
            {
                SceneManager.UnloadSceneAsync(path).completed += (asyncresult) =>
                {
                    training = false;
                };
            });

            // and let it go...

            while (training)
            {
                OnTrainingFrame?.Invoke(request, manager);
                yield return null;
            }

            OnTrainingRequestComplete?.Invoke(manager);
        }
    }
}
