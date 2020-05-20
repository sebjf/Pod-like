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
public class ProfileAgentManager : MonoBehaviour
{
    public class Experience
    {
        public TrackPath path;
        public List<List<ProfileAgent.Node>> profiles;

        public Experience(TrackPath path)
        {
            this.path = path;
            this.profiles = new List<List<ProfileAgent.Node>>();
        }
    }

    [Serializable]
    public class Agent
    {
        public ProfileAgent profilefinder;
        public Experience collector;

        public Agent(ProfileAgent pathfinder, Experience collector)
        {
            this.profilefinder = pathfinder;
            this.collector = collector;
        }
    }

    public int profileLength = 50;          // distance to sample using pathfinder in intervals
    public float profileSpeedStepSize = 5;
    public float profileErrorThreshold = 1;

    public int AgentInterval = 25;   // distance between agents along track in m

    public GameObject AgentPrefab;

    public string directory;

    public string filename
    {
        get
        {
            try
            {
                var t = GetComponentInChildren<Track>().Name.ToLower();
                var n = AgentPrefab.GetComponentInChildren<ProfileController>().modelName.ToLower();
                var fn = string.Format("{0}.{1}.trainingprofile.json", n, t);
                var d = Application.dataPath;
                return Path.Combine(directory, fn);
            }catch
            {
                return null;
            }
        }
    }


    private List<Agent> agents;
    private List<Agent> completed;
    private List<Experience> experiences;

    private PathVolume[] volumes;

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
    ProfileAgentManager()
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
        completed = new List<Agent>();
        agents = new List<Agent>();
        experiences = new List<Experience>();
        volumes = FindObjectsOfType<PathVolume>();
    }

    private void Reset()
    {
        
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
            if(agent.profilefinder.complete)
            {
                completed.Add(agent);
            }
        }

        foreach (var agent in completed)
        {
            agents.Remove(agent);
            agent.collector.profiles.Add(agent.profilefinder.profile);
            GameObject.Destroy(agent.profilefinder.gameObject);
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

    public ProfileAgent CreateAgent(TrackPath path, float position)
    {
        var container = path.transform.Find("Agents");
        if (!container)
        {
            container = new GameObject("Agents").transform;
            container.SetParent(path.transform);
        }

        Util.SetLayer(AgentPrefab, "Car"); // for now car but we may add a training car layer in the future

        var agent = GameObject.Instantiate(AgentPrefab, container);

        agent.name = agent.name + " " + agents.Count;

        var driver = agent.GetComponent<ProfileController>();
        DestroyImmediate(driver);

        var navigator = agent.GetComponent<Navigator>(); // ensure navigator is initialised first as pathfinder will reset it
        navigator.waypoints = path;
        navigator.StartingPosition = position;

        var pathfinder = agent.GetComponent<ProfileAgent>();

        if(!pathfinder)
        {
            pathfinder = agent.AddComponent<ProfileAgent>();
        }

        pathfinder.profileLength = profileLength;
        pathfinder.speedStepSize = profileSpeedStepSize;
        pathfinder.errorThreshold = profileErrorThreshold;
        pathfinder.interval = driver.observationsInterval;

        pathfinder.CreateProfile(); // re-create the profile with the updated parameters as CreateProfile will be called with the prefabs parameters by PathFinder.Awake() on AddComponent

        var reset = agent.GetComponent<ResetController>();
        reset.ResetPosition(position);

        agent.SetActive(true); // prefab may be disabled depending on when it was last updated

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
                    agents.Add(new Agent(CreateAgent(path, position), experienceCollector));
                }
            }
        }
    }

    public void OnComplete()
    {
        Export(filename);
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
            public float[] Sideslip;
            public float[] Braking;

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
                example.Sideslip = profile.Select(n => n.sideslip).ToArray();
                example.Braking = profile.Select(n => n.braking ? 1f : 0f).ToArray();

                example.Distance = new float[profile.Count];
                example.Curvature = new float[profile.Count];
                example.Camber = new float[profile.Count];
                example.Inclination = new float[profile.Count];
                for (int i = 0; i < profile.Count; i++)
                {
                    var node = profile[i];

                    var distance = node.distance;
                    example.Distance[i] = node.distance;

                    var Q = experienceset.path.Query(distance);
                    example.Curvature[i] = Q.Curvature;
                    example.Camber[i] = Q.Camber;
                    example.Inclination[i] = Q.Inclination;
                }

                Profiles.Add(example);
            }
        }
    }

}
