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

    public class SampleAgent
    {
        public TrackObservations trackobservations;
        public PathObservations pathobservations;
        public Autopilot autopilot;
        public Rigidbody body;
        public ResetController reset;

        public SampleAgent(MonoBehaviour component)
        {
            trackobservations = component.GetComponent<TrackObservations>();
            pathobservations = component.GetComponent<PathObservations>();
            autopilot = component.GetComponent<Autopilot>();
            reset = component.GetComponent<ResetController>();
            body = component.GetComponent<Rigidbody>();
            samples = new List<Sample>();
        }

        public List<Sample> samples;

        public void Sample(int step)
        {
            samples.Add(
                new Sample()
                {
                    time = Time.fixedTime,
                    step = step,
                    curvature = trackobservations.Curvature[0],
                    camber = trackobservations.Camber[0],
                    inclination = trackobservations.Inclination[0],
                    speed = body.velocity.magnitude,
                    height = pathobservations.height,
                    error = pathobservations.lateralError
                });
        }
    }

    public float SpeedMin;
    public float SpeedMax;
    public float SpeedStep;

    public float StepTime;

    private float speed;
    private int stepcounter;

    private List<SampleAgent> agents;

    public float TimeRemaining
    {
        get
        {
            return Mathf.FloorToInt((SpeedMax - speed) / SpeedStep) * StepTime;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        agents = GetComponentsInChildren<PathObservations>().Select(c => new SampleAgent(c)).ToList();
        speed = SpeedMin;
        StartCoroutine(ExperienceWorker());
    }

    private void FixedUpdate()
    {
        foreach (var agent in agents)
        {
            agent.Sample(stepcounter);
        }
        stepcounter++;
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
            stepcounter = 0;

            yield return new WaitForSeconds(StepTime);
        } while (speed <= SpeedMax);

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;     //https://answers.unity.com/questions/161858/
#elif UNITY_WEBPLAYER
            Application.OpenURL(webplayerQuitURL);
#else
            Application.Quit();
#endif
    }

}
