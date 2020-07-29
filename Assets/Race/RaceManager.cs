using Onnx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Barracuda;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[Serializable]
public class RaceConfiguration
{
    public Circuit circuit;
    public List<Car> cars;
    public Car player;
    public float difficulty;
    public int laps;
}

public enum Penalties
{
    Jumpstart,
}

public class Competitor
{
    public Vehicle vehicle;
    public TrackNavigator navigator;

    public bool finished;
    public float interval;
    public float reaction;

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
        reaction = -1;
    }
}

public class Race
{
    public List<Competitor> competitors;
    public int laps;

    public bool finished;

    public Race()
    {
        competitors = new List<Competitor>();
        laps = 0;
        finished = false;
    }

    public void Update()
    {
        for (int i = 0; i < competitors.Count; i++)
        {
            if(competitors[i].navigator.Lap > laps)
            {
                competitors[i].finished = true;
            }
        }

        finished = true;
        for (int i = 0; i < competitors.Count; i++)
        {
            if(!competitors[i].finished)
            {
                finished = false;
            }
        }

        for (int i = 0; i < competitors.Count; i++)
        {
            if(competitors[i].finished)
            {
                continue;
            }

            for (int j = i; j < competitors.Count; j++)
            {
                if(competitors[j].raceDistance > competitors[i].raceDistance)
                {
                    competitors.Swap(i, j);
                }
            }
        }

        // update the interval between drivers

        if(competitors.Count > 0)
        {
            competitors[0].interval = -1;
        }

        for (int i = 1; i < competitors.Count; i++)
        {
            var d = competitors[i - 1].raceDistance - competitors[i].raceDistance;
            var v = competitors[i].speed;
            competitors[i].interval = d / v;
        }
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
    public Catalogue catalogue;

    public RaceConfiguration configuration;

    public int gridForward;
    public int gridSideways;

    [NonSerialized]
    public Race race;
    public Competitor player;

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
        race = new Race();

        if (OnPenalty == null)
        {
            OnPenalty = new Penalty();
        }
    }

    public void ConfigureRace()
    {
        ConfigureRace(configuration);
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

        race = new Race();
        race.laps = config.laps;

        int i = 0;
        for (; i < cars.Count; i++)
        {
            var item = cars[i];
            var car = GameObject.Instantiate(item.Agent, geometry.transform);
            ResetController.PlacePrefab(car.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            race.competitors.Add(new Competitor(car));
        }

        if (config.player != null)
        {
            var player = GameObject.Instantiate(config.player.Player, geometry.transform);
            ResetController.PlacePrefab(player.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            this.player = new Competitor(player);
            race.competitors.Add(this.player);
        }

        foreach (var item in race.competitors)
        {
            item.navigator.Lap = 0;
        }

        raceCamera.cameraRigs = race.competitors.Select(c => c.vehicle.GetComponentInChildren<CamRig>()).ToArray();
        raceCamera.Target = race.competitors.Last().vehicle.GetComponent<CamRig>();

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

            foreach (var item in race.competitors)
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

            foreach (var item in race.competitors)
            {
                if(item.reaction < 0)
                {
                    if(item.vehicle.speed > 0.5f)
                    {
                        item.reaction = raceTime;
                        Debug.Log(item.vehicle.name + " Reaction Time: " + item.reaction.ToString());
                    }
                }
            }

            race.Update();

            if(race.finished)
            {
                break;
            }
            if(player.finished)
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
