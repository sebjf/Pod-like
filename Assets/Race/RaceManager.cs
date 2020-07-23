using Onnx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Barracuda;
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
    public List<GameObject> competitors;

    [NonSerialized]
    public RaceStage state;

    [NonSerialized]
    public int countdown;

    private void Awake()
    {
        competitors = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
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
        state = RaceStage.Preparation;

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

        competitors.Clear();

        int i = 0;
        for (; i < cars.Count; i++)
        {
            var item = cars[i];
            var car = GameObject.Instantiate(item.Agent, geometry.transform);
            ResetController.PlacePrefab(car.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            competitors.Add(car);
        }

        if (config.player != null)
        {
            var player = GameObject.Instantiate(config.player.Player, geometry.transform);
            ResetController.PlacePrefab(player.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            competitors.Add(player);
        }

        raceCamera.cameraRigs = competitors.Select(g => g.GetComponentInChildren<CamRig>()).ToArray();
        raceCamera.Target = competitors.Last().GetComponent<CamRig>();

        SendMessage("OnRacePrepared", this, SendMessageOptions.DontRequireReceiver);

        StartCoroutine(CountdownWorker());
    }

    private IEnumerator CountdownWorker()
    {
        state = RaceStage.Countdown;
        for (int i = 3; i > 0; i--)
        {
            countdown = i;
            yield return new WaitForSeconds(1f);
        }
        StartCoroutine(RaceWorker());
    }

    private IEnumerator RaceWorker()
    {
        state = RaceStage.Race;
        yield break;
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
