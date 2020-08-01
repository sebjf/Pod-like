using Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardBindings : MonoBehaviour
{
    public LeaderboardEntryBindings[] bindings;

    private class LeaderboardEntryComparison : IComparer<LeaderboardEntry>
    {
        public int Compare(LeaderboardEntry x, LeaderboardEntry y)
        {
            return x.place.CompareTo(y.place);
        }
    }

    private LeaderboardEntryComparison comparer = new LeaderboardEntryComparison();

    // Update is called once per frame
    void Update()
    {
        var leaderboard = GameManager.Instance.configuration.leaderboard;

        int i = 0;
        if (leaderboard != null)
        {
            leaderboard.Sort(comparer);

            for (; i < leaderboard.Count; i++)
            {
                bindings[i].UpdateEntry(leaderboard[i]);
                bindings[i].gameObject.SetActive(true);
            }
        }
        for (; i < bindings.Length; i++)
        {
            bindings[i].gameObject.SetActive(false);
        }
    }
}
