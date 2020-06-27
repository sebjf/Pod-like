using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Creates a set of agents that run different interpolations of a track
/// </summary>
[RequireComponent(typeof(TimeController))]
public class PathAgentManager : MonoBehaviour, IAgentManager
{
    public GameObject AgentPrefab;

    public class Agent
    {
        public Navigator navigator;
        public InterpolatedPath path;
        public float[] times;
    }

    public List<Agent> agents;
    public List<Agent> completed;
    public List<Agent> experiences;

    public IEnumerable<Transform> Agents
    {
        get
        {
            if (agents != null)
            {
                foreach (var item in agents.Select(a => a.navigator.transform))
                {
                    yield return item;
                }
            }
        }
    }

    public AgentManagerComplete OnComplete { get; private set; }

    public string Filename
    {
        get
        {
            var t = GetComponentInChildren<Track>().Name.ToLower();
            var n = AgentPrefab.GetComponentInChildren<ProfileController>().modelName.ToLower();
            var fn = string.Format("{0}.{1}.pathtimings.json", n, t);
            return fn;
        }
    }

    private void Awake()
    {
        if(OnComplete == null)
        {
            OnComplete = new AgentManagerComplete();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateAgents();
        var timeController = GetComponent<TimeController>();
        timeController.enableCapture = true;
    }

    public void CreateAgents()
    {
        agents = new List<Agent>();
        completed = new List<Agent>();
        experiences = new List<Agent>();

        var path = GetComponent<TrackGeometry>();
        for (float i = 0;  i < 1f; i += 0.1f)
        {
            agents.Add(CreateAgent(path, i));
        }

        experiences.AddRange(agents);
    }

    public Agent CreateAgent(TrackPath track, float coefficient)
    {
        var container = track.transform.Find("Agents");
        if (!container)
        {
            container = new GameObject("Agents").transform;
            container.SetParent(track.transform);
        }

        var agent = GameObject.Instantiate(AgentPrefab, container);
        var path = agent.gameObject.AddComponent<InterpolatedPath>();
        path.coefficient = coefficient;
        path.Initialise();

        var navigator = agent.GetComponent<Navigator>();
        navigator.waypoints = path;
        navigator.StartingPosition = 0;

        var reset = agent.GetComponent<ResetController>();
        reset.ResetPosition();

        agent.SetActive(true); // prefab may be disabled depending on when it was last updated
        SetLayer(agent, TrainingProperties.AgentLayer); // for now car but we may add a training car layer in the future

        return new Agent()
        {
            navigator = navigator,
            path = path,
            times = new float[path.waypoints.Count]
        };
    }

    static void SetLayer(GameObject obj, string layer)
    {
        foreach (Transform trans in obj.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }

    private void FixedUpdate()
    {
        foreach (var agent in agents)
        {
            agent.times[agent.path.WaypointQuery(agent.navigator.PathDistance).waypoint.index] = Time.time;

            if(agent.navigator.Lap >= 1)
            {
                completed.Add(agent);
            }
        }

        foreach (var agent in completed)
        {
            agents.Remove(agent);
            GameObject.Destroy(agent.navigator.gameObject);
            Debug.Log("Finish Time: " + agent.path.coefficient + " " + Time.time + " s");
        }

        completed.Clear();

        if (agents.Count <= 0)
        {
            if(OnComplete != null)
            {
                OnComplete.Invoke(this);
            }
        }
    }

    public void Export(string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(ExportJson());
            }
        }
    }

    public string ExportJson()
    {
        Experience experience = new Experience();

        var example = experiences.First();
        experience.crossoverIndices = example.path.crossoverPoints.ToArray();
        experience.profiles = new List<Profile>();

        foreach (var agent in experiences)
        {
            experience.profiles.Add(new Profile()
            {
                coefficient = agent.path.coefficient,
                times = agent.times
            });
        }

        return JsonUtility.ToJson(experience, true);
    }

    public void SetAgentPrefab(GameObject prefab)
    {
        this.AgentPrefab = prefab;    
    }

    [Serializable]
    public class Profile
    {
        public float coefficient;
        public float[] times;
    }

    [Serializable]
    public class Experience
    {
        public int[] crossoverIndices;
        public List<Profile> profiles;
    }

}
