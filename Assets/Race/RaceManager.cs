using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Game;
using System.Net.Http.Headers;

[Serializable]
public class RaceConfiguration
{
    public Circuit circuit;
    public List<Car> cars;
    public Car player;
    public float difficulty;
    public int laps;

    /// <summary>
    /// This is reinitialised by the Race Configuration worker
    /// </summary>
    public List<LeaderboardEntry> leaderboard;
}

public enum Penalties
{
    Jumpstart,
}

[Serializable]
public class LeaderboardEntry
{
    public string driver;
    public string car;
    public float reaction;
    public float interval;
    public float bestLap;
    public int place;
}

public class Competitor
{
    public Vehicle vehicle;
    public TrackNavigator navigator;

    public LeaderboardEntry entry;

    public bool finished;

    public Util.Trigger<int> onLapTrigger;
    public float lapStartTime;

    public float raceDistance
    {
        get
        {
            return (Mathf.Max(0, navigator.Lap) * navigator.waypoints.totalLength) + navigator.Distance;
        }
    }

    public float speed
    {
        get
        {
            return vehicle.speed;
        }
    }

    public List<Penalties> penalties;

    public Competitor(GameObject car)
    {
        vehicle = car.GetComponent<Vehicle>();
        navigator = car.GetComponent<TrackNavigator>();
        finished = false;
        penalties = new List<Penalties>();
        onLapTrigger = new Util.Trigger<int>();
    }
}

public enum RaceStage
{
    Preparation,
    Countdown,
    Race,
    Finish
}

public class RaceManager : MonoBehaviour
{
    public RaceCamera raceCamera;

    public int gridForward;
    public int gridSideways;

    public List<Competitor> competitors;
    public Competitor player;

    [NonSerialized]
    public int laps;

    [NonSerialized]
    public RaceStage stage;

    [NonSerialized]
    public int countdown;

    [NonSerialized]
    public float raceTime;

    public class Penalty : UnityEvent<Competitor> { }
    public Penalty OnPenalty;

    private void Awake()
    {
        if (OnPenalty == null)
        {
            OnPenalty = new Penalty();
        }
        competitors = new List<Competitor>();
    }

    public void ConfigureRace()
    {
        ConfigureRace(GameManager.Instance.configuration);
    }

    public void ConfigureRace(RaceConfiguration config)
    {
        StartCoroutine(ConfigureRaceWorker(config));
    }

    private IEnumerator ConfigureRaceWorker(RaceConfiguration config)
    {
        stage = RaceStage.Preparation;

        var operation = SceneManager.LoadSceneAsync(config.circuit.sceneName, LoadSceneMode.Additive);
        while(!operation.isDone)
        {
            yield return null;
        }

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(config.circuit.sceneName));

        var scene = SceneManager.GetSceneByName(config.circuit.sceneName);
        var track = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<Track>()).Where(o => o != null).First();
        var geometry = track.GetComponent<TrackGeometry>();

        var cars = config.cars;

        competitors = new List<Competitor>();
        laps = config.laps;

        int i = 0;
        for (; i < cars.Count; i++)
        {
            var item = cars[i];
            var car = GameObject.Instantiate(item.Agent, geometry.transform);
            ResetController.PlacePrefab(car.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            competitors.Add(new Competitor(car));
        }

        if (config.player != null)
        {
            var player = GameObject.Instantiate(config.player.Player, geometry.transform);
            ResetController.PlacePrefab(player.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            this.player = new Competitor(player);
            competitors.Add(this.player);
        }

        foreach (var item in competitors)
        {
            item.navigator.Lap = 0;
        }

        config.leaderboard.Clear();
        foreach (var item in competitors)
        {
            var entry = new LeaderboardEntry();
            entry.car = item.vehicle.name;
            entry.bestLap = float.PositiveInfinity;
            entry.place = competitors.IndexOf(item) + 1;
            
            if(item.vehicle.GetComponent<Agent>())
            {
                entry.driver = item.vehicle.GetComponent<Agent>().DriverName;
            }
            else
            {
                entry.driver = "Player 1";
            }

            config.leaderboard.Add(entry);
            item.entry = entry;
        }

        raceCamera.cameraRigs = competitors.Select(c => c.vehicle.GetComponentInChildren<CamRig>()).ToArray();
        raceCamera.Target = competitors.Last().vehicle.GetComponent<CamRig>();

        SendMessage("OnRacePrepared", this, SendMessageOptions.DontRequireReceiver);

        StartCoroutine(CountdownWorker());
    }

    private IEnumerator CountdownWorker()
    {
        stage = RaceStage.Countdown;
        var time = 3f;
        do
        {
            time -= Time.deltaTime;
            countdown = Mathf.FloorToInt(time) + 1;

            foreach (var item in competitors)
            {
                if(item.navigator.TotalDistanceTravelled > 1f)
                {
                    item.penalties.Add(Penalties.Jumpstart);
                    item.navigator.TotalDistanceTravelled = 0f;
                    OnPenalty.Invoke(item);
                }
            }

            yield return null;
        } while (time > 0);
        StartCoroutine(RaceWorker());
    }

    private IEnumerator RaceWorker()
    {
        stage = RaceStage.Race;
        while (true)
        {
            raceTime += Time.deltaTime;

            foreach (var item in competitors)
            {
                if(item.entry.reaction < 0)
                {
                    if(item.vehicle.speed > 0.5f)
                    {
                        item.entry.reaction = raceTime;
                        Debug.Log(item.vehicle.name + " Reaction Time: " + item.entry.reaction.ToString());
                    }
                }

                item.onLapTrigger.Update(item.navigator.Lap);
                if (item.onLapTrigger.Changed)
                {
                    if (item.navigator.Lap > 1)
                    {
                        var lapTime = raceTime - item.lapStartTime;
                        item.entry.bestLap = Mathf.Min(item.entry.bestLap, lapTime);
                    }

                    item.lapStartTime = raceTime;
                }
            }

            for (int i = 0; i < competitors.Count; i++)
            {
                if (competitors[i].navigator.Lap > laps)
                {
                    competitors[i].finished = true;
                }
            }

            bool finished = true;
            for (int i = 0; i < competitors.Count; i++)
            {
                if (!competitors[i].finished)
                {
                    finished = false;
                }
            }
            if(player.finished)
            {
                finished = true;
            }

            for (int i = 0; i < competitors.Count; i++)
            {
                if (competitors[i].finished)
                {
                    continue;
                }

                for (int j = i; j < competitors.Count; j++)
                {
                    if (competitors[j].raceDistance > competitors[i].raceDistance)
                    {
                        competitors.Swap(i, j);
                    }
                }
            }

            for (int i = 0; i < competitors.Count; i++)
            {
                competitors[i].entry.place = i + 1;
            }

            if (competitors.Count > 0)
            {
                competitors[0].entry.interval = -1;
            }

            for (int i = 1; i < competitors.Count; i++)
            {
                var d = competitors[i - 1].raceDistance - competitors[i].raceDistance;
                var v = competitors[i].speed;
                competitors[i].entry.interval = d / v;
            }

            if (finished)
            {
                break;
            }

            yield return null;
        }
        StartCoroutine(FinishWorker());
    }

    private IEnumerator FinishWorker()
    {
        stage = RaceStage.Finish;
        yield return null;
    }

    private float GridStartDistance(int position)
    {
        return -(gridForward * (position / 2)) - gridForward;
    }

    private float GridStartOffset(int position)
    {
        return -gridSideways * (position % 2);
    }
    
}
