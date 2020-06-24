using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static TrainingManager;

[RequireComponent(typeof(TrainingManager))]
public class TrainingVisualiser : MonoBehaviour
{
    private List<GameObject> avatars;
    private AsyncOperation changeSceneOperation;
    private string currentScene;
    private string currentAgentPrefabKey;

    private TrainingManager manager;

    private void Awake()
    {
        manager = GetComponentInParent<TrainingManager>();
        avatars = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        lock (manager.state)
        {
            if (changeSceneOperation != null)
            {
                if (changeSceneOperation.isDone)
                {
                    changeSceneOperation = null;
                }
            }

            if (changeSceneOperation == null)
            {
                if (currentScene != manager.state.scenekey)
                {
                    if (currentScene != null)
                    {
                        changeSceneOperation = SceneManager.UnloadSceneAsync(currentScene);
                        changeSceneOperation.completed += (operation) =>
                        {
                            currentScene = null;
                        };
                    }
                    else
                    {
                        var scene = manager.state.scenekey;
                        changeSceneOperation = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                        changeSceneOperation.completed += (operation) =>
                        {
                            currentScene = scene;
                        };
                    }
                }
            }

            if (currentAgentPrefabKey != manager.state.carkey)
            {
                foreach (var item in avatars)
                {
                    Destroy(item);
                }

                avatars.Clear();
                currentAgentPrefabKey = manager.state.carkey;
            }

            if (currentAgentPrefabKey != null)
            {
                while (avatars.Count > manager.state.agents.Count)
                {
                    var go = avatars.Last();
                    avatars.Remove(go);
                    Destroy(go);
                }

                while (avatars.Count < manager.state.agents.Count)
                {
                    var go = GameObject.Instantiate(manager.GetCarPrefab(currentAgentPrefabKey), transform);
                    avatars.Add(go);

                    foreach (var item in go.GetComponentsInChildren<ProfileController>())
                    {
                        DestroyImmediate(item);
                    }
                    foreach (var item in go.GetComponentsInChildren<Vehicle>())
                    {
                        DestroyImmediate(item);
                    }
                    foreach (var item in go.GetComponentsInChildren<Wheel>())
                    {
                        DestroyImmediate(item);
                    }
                    foreach (var item in go.GetComponentsInChildren<Rigidbody>())
                    {
                        DestroyImmediate(item);
                    }
                    foreach (var item in go.GetComponentsInChildren<Collider>())
                    {
                        DestroyImmediate(item);
                    }
                    foreach (var item in go.GetComponentsInChildren<Autopilot>())
                    {
                        DestroyImmediate(item);
                    }
                    // and anything else to turn this into a husk...
                }

                for (int i = 0; i < manager.state.agents.Count; i++)
                {
                    avatars[i].transform.position = manager.state.agents[i].position;
                    avatars[i].transform.rotation = manager.state.agents[i].rotation;
                }
            }
        }
    }
}
