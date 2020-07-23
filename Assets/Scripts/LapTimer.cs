using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapTimer : MonoBehaviour
{
    private TrackNavigator navigator;

    private float startTime;
    private float previousLap;

    private void Awake()
    {
        navigator = GetComponent<TrackNavigator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
        previousLap = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (navigator.Lap != previousLap)
        {
            if (navigator.Lap == 0)
            {
                startTime = Time.time;
            }

            if (navigator.Lap > 0)
            {
                Debug.Log(name + " Lap Time: " + (Time.time - startTime));
                startTime = Time.time;
            }
        }

        previousLap = navigator.Lap;
    }
}
