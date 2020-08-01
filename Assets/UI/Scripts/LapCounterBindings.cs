using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LapCounterBindings : MonoBehaviour
{
    public RaceManager manager;
    public Text lapCounter;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int lap = 0;
        if (manager.player != null)
        {
            lap = Math.Max(1, manager.player.navigator.Lap);
        }
        lapCounter.text = string.Format("Lap {0}/{1}", Mathf.Clamp(lap, 0, manager.laps), manager.laps);
    }
}
