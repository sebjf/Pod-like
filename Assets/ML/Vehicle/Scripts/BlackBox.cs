using MLAgents;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BlackBox : MonoBehaviour
{
    // Start is called before the first frame update

    public string filename;

    private StreamWriter stream;

    private int stepcount;

    public bool logReward = false;
    public bool logCurvature = false;

    private void Awake()
    {
       
    }

    void Start()
    {
        if(filename != null && filename != "")
        {
            stream = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write));
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void FixedUpdate()
    {
        stepcount++;

        if (stream != null)
        {
            if (logReward)
            {
                var agent = GetComponent<Agent>();
                stream.WriteLine("{0}, {1}, {2}, {3}", stepcount, Time.fixedTime, agent.GetReward(), agent.GetCumulativeReward());
            }
        }

        if (logCurvature)
        {
            var agent = GetComponent<VehicleAgentAriadne>();
            var waypoints = agent.waypoints;
            var numObservations = agent.numObservations;
            var navigator = agent.navigator;
            var pathInterval = agent.pathInterval;
            if (waypoints != null)
            {
                var graph = FindObjectOfType<GraphOverlay>();
                if (graph != null)
                {
                    graph.widthSeconds = Time.fixedDeltaTime * numObservations;
                    var series = graph.GetSeries("Curvatures");
                    series.values.Clear();
                    for (int i = 0; i < numObservations; i++)
                    {
                        series.values.Add(waypoints.Curvature(navigator.TrackDistance + i * pathInterval) * 5f);
                    }

                    series = graph.GetSeries("Widths");
                    series.values.Clear();
                    for (int i = 0; i < numObservations; i++)
                    {
                        series.values.Add(waypoints.Width(navigator.TrackDistance + i * pathInterval) * 0.01f);
                    }
                }
            }

        }
    }

    private void OnDestroy()
    {
        if (stream != null)
        {
            stream.Close();
        }
    }
}
