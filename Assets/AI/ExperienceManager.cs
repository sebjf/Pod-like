using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the child agents to collect experiences based on sample distributions of speed and paths. 
/// This version assumes the cars have already been distributed across tracks.
/// </summary>
public class ExperienceManager : MonoBehaviour
{
    public struct Sample
    {
        float curvature;
        float camber;
        float inclination;
        float speed;
        float traction;
        float error;
    }

    public class Agent
    {
        public TrackObservations trackobservations;
        public PathObservations pathobservations;
        public Autopilot autopilot;
        public ResetController reset;

        public Agent(MonoBehaviour component)
        {
            trackobservations = component.GetComponent<TrackObservations>();
            pathobservations = component.GetComponent<PathObservations>();
            autopilot = component.GetComponent<Autopilot>();
            reset = component.GetComponent<ResetController>();
        }
    }

    public float SpeedMin;
    public float SpeedMax;
    public float SpeedStep;

    public float StepTime;

    public float speed;

    private List<Agent> agents;

    // Start is called before the first frame update
    void Start()
    {
        agents = GetComponentsInChildren<PathObservations>().Select(c => new Agent(c)).ToList();
        speed = SpeedMin;
        StartCoroutine(ExperienceWorker());
    }

    private void FixedUpdate()
    {
        // collect data here
    }

    private IEnumerator ExperienceWorker()
    {
        do
        {
            foreach (var item in agents)
            {
                item.reset.ResetPosition();
                item.autopilot.speed = speed;
            }

            speed += SpeedStep;

            yield return new WaitForSeconds(StepTime);
        } while (speed <= SpeedMax);

        //https://answers.unity.com/questions/161858/startstop-playmode-from-editor-script.html

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
        #else
            Application.Quit();
        #endif
    }

}
