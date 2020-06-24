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
    [Serializable]
    public struct AgentTransform
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    public struct TrainingState
    {
        public string scene;
        public string agentprefabkey;
        public List<AgentTransform> agents;
    }

    private TrainingState state;

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
        if(changeSceneOperation != null)
        {
            if(changeSceneOperation.isDone)
            {
                changeSceneOperation = null;
            }
        }

        if(changeSceneOperation == null)
        {
            if(currentScene != state.scene)
            {
                if(currentScene != null)
                {
                    changeSceneOperation = SceneManager.UnloadSceneAsync(currentScene);
                    changeSceneOperation.completed += (operation) =>
                    {
                        currentScene = null;
                    };
                }
                else
                {
                    var scene = state.scene;
                    changeSceneOperation = SceneManager.LoadSceneAsync(scene);
                    changeSceneOperation.completed += (operation) =>
                    {
                        currentScene = scene;
                    };
                }
            }
        }

        if(currentAgentPrefabKey != state.agentprefabkey)
        {
            foreach (var item in avatars)
            {
                Destroy(item);
            }

            avatars.Clear();
            currentAgentPrefabKey = state.agentprefabkey;
        }

        if (currentAgentPrefabKey != null)
        {
            while (avatars.Count > state.agents.Count)
            {
                var go = avatars.Last();
                avatars.Remove(go);
                Destroy(go);
            }

            while (avatars.Count < state.agents.Count)
            {
                var go = GameObject.Instantiate(manager.GetCarPrefab(currentAgentPrefabKey), transform);
                avatars.Add(go);

                foreach (var item in go.GetComponentsInChildren<Vehicle>())
                {
                    Destroy(item);
                }
                foreach (var item in go.GetComponentsInChildren<Wheel>())
                {
                    Destroy(item);
                }
                foreach (var item in go.GetComponentsInChildren<Rigidbody>())
                {
                    Destroy(item);
                }
                foreach (var item in go.GetComponentsInChildren<ProfileController>())
                {
                    Destroy(item);
                }
                foreach (var item in go.GetComponentsInChildren<Collider>())
                {
                    Destroy(item);
                }
                // and anything else to turn this into a husk...
            }

            for (int i = 0; i < state.agents.Count; i++)
            {
                avatars[i].transform.position = state.agents[i].position;
                avatars[i].transform.rotation = state.agents[i].rotation;
            }
        }
    }
}
