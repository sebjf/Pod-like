using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Controls the child agents to collect experiences based on sample distributions of speed and paths. 
/// This version assumes cars have already been distributed across tracks and starting locations.
/// </summary>
[RequireComponent(typeof(TimeController))]
public class ProfileAgentManager : MonoBehaviour, IAgentManager
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
    public float profileSpeedStepSize = 2.5f;
    public float profileErrorThreshold = 1;
    public int autopilotLookahead = 15;
    public int AgentInterval = 50;   // distance between agents along track in m
    public GameObject AgentPrefab;
    public string directory = @"Support\Data";

    public void SetAgentPrefab(GameObject prefab)
    {
        this.AgentPrefab = prefab;
    }

    public AgentManagerComplete OnComplete { get; set; }

    public string Filename
    {
        get
        {
            var t = GetComponentInChildren<Track>().Name.ToLower();
            var n = AgentPrefab.GetComponentInChildren<ProfileController>().modelName.ToLower();
            var fn = string.Format("{0}.{1}.trainingprofile.json", n, t);
            return fn;
        }
    }

    public string fullfile
    {
        get
        {
            try
            {
                var fn = Filename;
                var d = Application.dataPath;
                return Path.GetFullPath(Path.Combine(directory, fn));
            }catch
            {
                return null;
            }
        }
    }

    private List<Agent> agents;
    private List<Agent> completed;
    private List<Experience> experiences;

    public IEnumerable<Transform> Agents
    {
        get
        {
            if (agents != null)
            {
                foreach (var item in agents.Select(a => a.profilefinder.transform))
                {
                    yield return item;
                }
            }
        }
    }

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
        completed = new List<Agent>();
        agents = new List<Agent>();
        experiences = new List<Experience>();
        if(OnComplete == null)
        {
            OnComplete = new AgentManagerComplete();
        }
        OnComplete.AddListener(InternalComplete);
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
            OnComplete.Invoke(this);
        }
    }

    public bool CheckPosition(TrackPath path, float position)
    {
        if(path.Flags(position).nospawn)
        {
            return false;
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

        Util.SetLayer(AgentPrefab, TrainingProperties.AgentLayer); // for now car but we may add a training car layer in the future

        var agent = GameObject.Instantiate(AgentPrefab, container);

        agent.name = agent.name + " " + agents.Count;

        var driver = agent.GetComponent<ProfileController>();
        DestroyImmediate(driver);

        var navigator = agent.GetComponent<PathNavigator>(); // ensure navigator is initialised first as profile agent will reset it
        navigator.waypoints = path;
        navigator.StartingPosition = position;

        var tracknavigator = agent.GetComponentInChildren<TrackNavigator>();
        if(tracknavigator != null)
        {
            DestroyImmediate(tracknavigator); // don't need this so dont waste cpu time
        }

        var profileAgent = agent.GetComponent<ProfileAgent>();

        if(!profileAgent)
        {
            profileAgent = agent.AddComponent<ProfileAgent>();
        }

        profileAgent.profileLength = profileLength;
        profileAgent.speedStepSize = profileSpeedStepSize;
        profileAgent.interval = driver.observationsInterval;

        profileAgent.CreateProfile(); // re-create the profile with the updated parameters as CreateProfile will be called with the prefabs parameters by PathFinder.Awake() on AddComponent
        profileAgent.Reset();

        agent.SetActive(true); // prefab may be disabled depending on when it was last updated

        return profileAgent;
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

    public void InternalComplete(IAgentManager manager)
    {
        Export();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;     //https://answers.unity.com/questions/161858/
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
    }

    public string ExportJson()
    {
        var experienceDataset = new ExperienceDataset();
        foreach (var item in experiences)
        {
            experienceDataset.Add(item);
        }
        return JsonUtility.ToJson(experienceDataset, true);
    }

    public void ExportJson(Stream output)
    {
        using (StreamWriter writer = new StreamWriter(output))
        {
            writer.Write(ExportJson());
        }
    }

    public void Export()
    {
        Export(fullfile);
    }

    public void Export(string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            ExportJson(stream);
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
