using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntryBindings : MonoBehaviour
{
    public Text position;
    public Text driverName;
    public Text carName;
    public Text interval;
    public Text bestLap;
    public Text reactionTime;

    public void UpdateEntry(LeaderboardEntry entry)
    {
        if (position != null)
        {
            position.text = entry.place.ToString();
        }
        if (driverName != null)
        {
            driverName.text = entry.driver;
        }
        if (carName != null)
        {
            carName.text = entry.car;
        }
        if (interval != null)
        {
            interval.text = string.Format("{0:0.00}", entry.interval);
        }
        if (bestLap != null)
        {
            bestLap.text = string.Format("{0:0.00}", entry.bestLap);
        }
    }
}
