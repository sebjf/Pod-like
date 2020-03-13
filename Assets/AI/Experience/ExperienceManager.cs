using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the child agents to collect experiences based on sample distributions of speed and paths. 
/// This version assumes cars have already been distributed across tracks and starting locations.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public struct Sample
    {
        public float time;
        public int step;
        public float curvature;
        public float camber;
        public float inclination;
        public float speed;
        public float height;
        public float error;
    }

    [Serializable]
    public class AgentManager
    {
        public PathFinder pathfinder;

        public AgentManager(PathFinder pathfinder)
        {
            this.pathfinder = pathfinder;
        }
    }

    public int profileInterval = 10;        // distance between intervals for pathfinder in m
    public int profileLength = 40;          // distance to sample using pathfinder in intervals
    public float profileSpeedStepSize = 5;
    public float profileErrorThreshold = 1;

    public int AgentInterval = 20;   // distance between agents along track in m

    public GameObject AgentPrefab;

    private List<AgentManager> agents;
    private List<AgentManager> completed;

    [NonSerialized]
    public float elapsedRealTime;

    [NonSerialized]
    public float elapsedVirtualTime;


    public int AgentsRemaining
    {
        get
        {
            if(agents != null)
            {
                return agents.Count;
            }
            else
            {
                return 0;
            }
        }
    }

    private void Awake()
    {
        completed = new List<AgentManager>();
        agents = new List<AgentManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateAgents();
    }

    private void FixedUpdate()
    {
        foreach (var agent in agents)
        {
            if(agent.pathfinder.complete)
            {
                completed.Add(agent);
            }
        }

        foreach (var agent in completed)
        {
            agents.Remove(agent);
            GameObject.Destroy(agent.pathfinder.gameObject);
        }

        completed.Clear();

        elapsedRealTime += Time.unscaledDeltaTime;
        elapsedVirtualTime += Time.deltaTime;

        if (agents.Count <= 0)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;     //https://answers.unity.com/questions/161858/
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
        }
    }

    public AgentManager CreateAgent(TrackPath path, float position)
    {
        var container = path.transform.Find("Agents");
        if (!container)
        {
            container = new GameObject("Agents").transform;
            container.SetParent(path.transform);
        }

        var agent = GameObject.Instantiate(AgentPrefab, container);

        var navigator = agent.GetComponent<Navigator>();
        navigator.waypoints = path;

        var pathfinder = agent.GetComponent<PathFinder>();
        pathfinder.profileLength = profileLength;
        pathfinder.interval = profileInterval;
        pathfinder.speedStepSize = profileSpeedStepSize;
        pathfinder.errorThreshold = profileErrorThreshold;

        var reset = agent.GetComponent<ResetController>();
        reset.ResetPosition(position);

        agent.SetActive(true); // prefab may be disabled depending on when it was last updated

        SetLayer(agent, "Car"); // for now car but we may add a training car layer in the future

        return new AgentManager(pathfinder);
    }

    public void CreateAgents()
    {
        foreach (var path in GetComponentsInChildren<TrackPath>())
        {
            // ignore geometries - a derived trackwith with a more regular sampling should represent this
            if(path is TrackGeometry)
            {
                continue;
            }

            for (float position = 0; position < path.totalLength; position += AgentInterval)
            {
                agents.Add(CreateAgent(path, position));
            }
        }
    }

    static void SetLayer(GameObject obj, string layer)
    {
        foreach (Transform trans in obj.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }
}
