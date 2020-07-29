using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardBindings : MonoBehaviour
{
    public RaceManager manager;

    public Text lapIndicator;
    public Text leaderboard;



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
        lapIndicator.text = string.Format("Lap {0}/{1}", lap, manager.race.laps);

        string rtf = "";
        foreach (var item in manager.race.competitors)
        {
            rtf += item.vehicle.gameObject.name + " " + item.interval + " s" + "\n";
        }
        leaderboard.text = rtf;
    }
}
