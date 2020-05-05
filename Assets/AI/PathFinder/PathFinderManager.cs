using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the child agents to collect experiences based on sample distributions of speed and paths. 
/// This version assumes cars have already been distributed across tracks and starting locations.
/// </summary>
[RequireComponent(typeof(TimeController))]
public class PathFinderManager : MonoBehaviour
{
    public class Experience
    {
        public TrackPath path;
        public List<List<PathFinder.Node>> profiles;

        public Experience(TrackPath path)
        {
            this.path = path;
            this.profiles = new List<List<PathFinder.Node>>();
        }
    }

    [Serializable]
    public class AgentManager
    {
        public PathFinder pathfinder;
        public Experience collector;

        public AgentManager(PathFinder pathfinder, Experience collector)
        {
            this.pathfinder = pathfinder;
            this.collector = collector;
        }
    }

    public int profileInterval = 10;        // distance between intervals for pathfinder in m
    public int profileLength = 50;          // distance to sample using pathfinder in intervals
    public float profileSpeedStepSize = 5;
    public float profileErrorThreshold = 1;

    public int AgentInterval = 25;   // distance between agents along track in m

    public GameObject AgentPrefab;

    public string file;

    private List<AgentManager> agents;
    private List<AgentManager> completed;
    private List<Experience> experiences;

    private PathFinderVolume[] volumes;

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

#if UNITY_EDITOR
    PathFinderManager()
    {
        // https://docs.unity3d.com/ScriptReference/EditorApplication-playModeStateChanged.html
        UnityEditor.EditorApplication.playModeStateChanged +=
            (UnityEditor.PlayModeStateChange state) =>
            {
                //enabled = false;
            };
    }
#endif

    private void Awake()
    {
        completed = new List<AgentManager>();
        agents = new List<AgentManager>();
        experiences = new List<Experience>();
        volumes = FindObjectsOfType<PathFinderVolume>();
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateAgents();
        var timeController = GetComponent<TimeController>();
        if (timeController)
        {
            timeController.enableCapture = true;
        }

        var graph = GameObject.Find("Graph");
        if(graph)
        {
            graph.SetActive(false);
        }

        var camera = GameObject.Find("Race Camera");
        if(camera)
        {
            camera.SetActive(false);
        }
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
            agent.collector.profiles.Add(agent.pathfinder.profile);
            GameObject.Destroy(agent.pathfinder.gameObject);
        }

        completed.Clear();

        elapsedRealTime += Time.unscaledDeltaTime;
        elapsedVirtualTime += Time.deltaTime;

        if (agents.Count <= 0)
        {
            OnComplete();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;     //https://answers.unity.com/questions/161858/
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
        }
    }

    public bool CheckPosition(TrackPath path, float position)
    {
        foreach (var item in volumes)
        {
            if (item.Excludes(path.Query(position).Midpoint))
            {
                return false;
            }
        }
        return true;
    }

    public PathFinder CreateAgent(TrackPath path, float position)
    {
        var container = path.transform.Find("Agents");
        if (!container)
        {
            container = new GameObject("Agents").transform;
            container.SetParent(path.transform);
        }

        var agent = GameObject.Instantiate(AgentPrefab, container);

        agent.name = agent.name + " " + agents.Count;

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

        return pathfinder;
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
            if(!path.enabled)
            {
                continue;
            }

            var experienceCollector = new Experience(path);
            experiences.Add(experienceCollector);

            for (float position = 0; position < path.totalLength; position += AgentInterval)
            {
                if (CheckPosition(path, position))
                {
                    agents.Add(new AgentManager(CreateAgent(path, position), experienceCollector));
                }
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

    public void OnComplete()
    {
        Export(file);
    }

    public void Export(string filename)
    {
        using(FileStream stream = new FileStream(filename, FileMode.Create))
        {
            ExportJson(stream);
        }
    }

    public void ExportJson(Stream output)
    {
        var experienceDataset = new ExperienceDataset();
        foreach (var item in experiences)
        {
            experienceDataset.Add(item);
        }
        using (StreamWriter writer = new StreamWriter(output))
        {
            writer.Write(JsonUtility.ToJson(experienceDataset, true));
        }
    }

    /// <summary>
    /// Collection of experience expressed as a training set
    /// </summary>
    [Serializable]
    public class ExperienceDataset
    {
        [Serializable]
        public class Profile
        {
            public string Path;

            public float[] Distance;
            public float[] Curvature;
            public float[] Camber;
            public float[] Inclination;

            public float[] Speed;
            public float[] Actual;
            public float[] Drift;

            public float[] Error;
        }

        public List<Profile> Profiles = new List<Profile>();

        public void Add(Experience experienceset)
        {
            foreach (var profile in experienceset.profiles)
            {
                var example = new Profile();
                
                example.Path = experienceset.path.UniqueName();

                example.Speed = profile.Select(n => n.speed).ToArray();
                example.Actual = profile.Select(n => n.actual).ToArray();
                example.Error = profile.Select(n => n.error).ToArray();
                example.Drift = profile.Select(n => n.drift).ToArray();

                var observationsPerNode = 1;

                var length = profile.Count * observationsPerNode;
                example.Distance = new float[length];
                example.Curvature = new float[length];
                example.Camber = new float[length];
                example.Inclination = new float[length];

                for (int i = 0; i < profile.Count; i++)
                {
                    var node = profile[i];
                    for (int d = 0; d < observationsPerNode; d++)
                    {
                        var distance = node.distance + d;
                        var index = (i * observationsPerNode) + d;
                        example.Distance[index] = node.distance;
                        var Q = experienceset.path.Query(distance);
                        example.Curvature[index] = Q.Curvature;
                        example.Camber[index] = Q.Camber;
                        example.Inclination[index] = Q.Inclination;
                    }
                }

                Profiles.Add(example);
            }
        }
    }

}
