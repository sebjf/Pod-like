using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Entrant
{
    public Vehicle vehicle;
    public TrackNavigator navigator;

    public float raceDistance
    {
        get
        {
            return (Mathf.Max(0, navigator.Lap) * navigator.waypoints.totalLength) + navigator.Distance;
        }
    }

    public Entrant(GameObject car)
    {
        vehicle = car.GetComponent<Vehicle>();
        navigator = car.GetComponent<TrackNavigator>();
    }
}

[RequireComponent(typeof(RaceManager))]
public class Leaderboard : MonoBehaviour, IComparer<Entrant>
{
    public List<Entrant> drivers;

    private void Awake()
    {
        drivers = new List<Entrant>();
    }

    public void OnRacePrepared(RaceManager manager)
    {
        drivers.Clear();
        drivers.AddRange(manager.competitors.Select(x => new Entrant(x)));
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public int Compare(Entrant x, Entrant y)
    {
        return y.raceDistance.CompareTo(x.raceDistance);
    }

    // Update is called once per frame
    void Update()
    {
        drivers.Sort(this);
    }
}
