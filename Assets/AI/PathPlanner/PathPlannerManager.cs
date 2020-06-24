using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PathPlannerManager : MonoBehaviour
{
    public GameObject AgentPrefab;
    public string file;

    public class Interpolation
    {
        public float[] genome;
        public float[] coefficients;
    }

    public class Agent
    {
        public Interpolation interpolation;
        public Navigator navigator;
        public InterpolatedPath path;
        public float[] times;
    }

    public List<Interpolation> interpolations; // interoplations to be tested

    public List<Agent> agents;
    public List<Agent> completed;
    public List<Agent> experiences;

    [HideInInspector]
    public InterpolatedPath path;

    [HideInInspector]
    public List<int> sections;

    private void Awake()
    {
        path = GetComponentInChildren<InterpolatedPath>();
        sections = path.crossoverPoints;
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateInterpolations();
        CreateAgents();
        var timeController = GetComponent<TimeController>();
        if (timeController)
        {
            timeController.enableCapture = true;
        }
    }

    public void CreateInterpolations()
    {
        interpolations = new List<Interpolation>();

        // grid search
        for (int c = 0; c < sections.Count; c++)
        {    
            for (float s = 0; s <= 1; s += 0.2f)
            {
                var interpolation = new Interpolation();

                interpolation.genome = new float[sections.Count];
                interpolation.coefficients = new float[path.waypoints.Count];

                for (int i = 0; i < interpolation.genome.Length; i++)
                {
                    interpolation.genome[i] = path.coefficient;
                }
                interpolation.genome[c] = s;

                // create coefficients

                for (int i = 0; i < interpolation.coefficients.Length; i++)
                {
                    var bin = 0; // when index is greater than the last crossover index, the statement will never be true, so following indices will take the value of zero, automatically wrapping
                    for (int j = 0; j < sections.Count; j++)
                    {
                        if(i < j)
                        {
                            bin = j;
                        }
                        else
                        {
                            break;
                        }
                    }

                    interpolation.coefficients[i] = interpolation.genome[bin];
                }


                interpolations.Add(interpolation);
            }
        }
    }

    public void CreateAgents()
    {
        agents = new List<Agent>();
        completed = new List<Agent>();
        experiences = new List<Agent>();

        foreach (var path in GetComponentsInChildren<InterpolatedPath>())
        {
            foreach (var item in interpolations)
            {
                agents.Add(CreateAgent(path, item));
            }
        }

        experiences.AddRange(agents);
    }

    public Agent CreateAgent(TrackPath track, Interpolation interpolation)
    {
        var container = track.transform.Find("Agents");
        if (!container)
        {
            container = new GameObject("Agents").transform;
            container.SetParent(track.transform);
        }

        var agent = GameObject.Instantiate(AgentPrefab, container);
        var path = agent.gameObject.AddComponent<InterpolatedPath>();
        path.Initialise();
        path.coefficients = interpolation.coefficients;
        path.Recompute();

        var navigator = agent.GetComponent<Navigator>();
        navigator.waypoints = path;
        navigator.StartingPosition = 0;

        var reset = agent.GetComponent<ResetController>();
        reset.ResetPosition();

        agent.SetActive(true); // prefab may be disabled depending on when it was last updated
        SetLayer(agent, "Car"); // for now car but we may add a training car layer in the future

        return new Agent()
        {
            navigator = navigator,
            interpolation = interpolation,
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

            if(agent.navigator.Lap == 1)
            {
                completed.Add(agent);
            }
        }

        foreach (var agent in completed)
        {
            agents.Remove(agent);
            GameObject.Destroy(agent.navigator.gameObject);
            Debug.Log("Finish Time: " + agent.interpolation.coefficients[0] + " " + Time.time + " s");
        }

        completed.Clear();

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

    public void OnComplete()
    {
        Export(file);
    }

    public void Export(string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            ExportJson(stream);
        }
    }

    public void ExportJson(Stream output)
    {
        Experience experience = new Experience();

        experience.sectionIndices = sections.ToArray();
        experience.sectionDistances = sections.Select(i => path.waypoints[i].Distance).ToArray();
        experience.timings = new List<Profile>();

        foreach (var agent in experiences)
        {
            experience.timings.Add(new Profile()
            {
                genome = agent.interpolation.genome,
                times = agent.times
            });
        }

        using (StreamWriter writer = new StreamWriter(output))
        {
            writer.Write(JsonUtility.ToJson(experience, true));
        }
    }

    [Serializable]
    public class Profile
    {
        public float[] genome;
        public float[] times;
    }

    [Serializable]
    public class Experience
    {
        public int[] sectionIndices;
        public float[] sectionDistances;
        public List<Profile> timings;
    }

}
