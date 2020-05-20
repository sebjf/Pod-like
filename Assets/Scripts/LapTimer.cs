using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LapTimer : MonoBehaviour
{
    private Navigator navigator;

    private float startTime;
    private float previousLap;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
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
        if(navigator.Lap > 0 && navigator.Lap != previousLap)
        {
            Debug.Log(name + " Lap Time: " + (Time.time - startTime));
            previousLap = navigator.Lap;
        }
    }
}
