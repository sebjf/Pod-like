using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Autopilot))]
public class Agent : MonoBehaviour
{
    public string DriverName;

    public float Difficulty;
    public RaceStage state = RaceStage.Race;

    private Autopilot autopilot;

    private void Awake()
    {
        autopilot = GetComponent<Autopilot>();
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case RaceStage.Race:
                autopilot.speedScalar = Mathf.Clamp(0.8f + (0.2f * Difficulty), 0.8f, 1f);
                break;
            default:
                autopilot.speedScalar = 0f;
                break;
        }
    }
}
