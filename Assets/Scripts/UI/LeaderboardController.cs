using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardController : MonoBehaviour
{
    public Leaderboard leaderboard;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        string rtf = "";
        foreach (var item in leaderboard.drivers)
        {
            rtf += item.vehicle.gameObject.name + "\n";
        }
        GetComponent<Text>().text = rtf;
    }
}
