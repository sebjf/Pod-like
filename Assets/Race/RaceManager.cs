﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class RaceConfiguration
{
    public Circuit circuit;
    public List<Car> cars;
    public Car player;
}

public class RaceManager : MonoBehaviour
{
    public RaceCamera raceCamera;
    public Catalogue catalogue;

    public RaceConfiguration configuration;

    public int gridForward;
    public int gridSideways;

    [NonSerialized]
    public List<GameObject> running;


    private void Awake()
    {
        running = new List<GameObject>();
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
        var operation = SceneManager.LoadSceneAsync(config.circuit.sceneName, LoadSceneMode.Additive);
        while(!operation.isDone)
        {
            yield return null;
        }

        var scene = SceneManager.GetSceneByName(config.circuit.sceneName);
        var track = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<Track>()).Where(o => o != null).First();
        var geometry = track.GetComponent<TrackGeometry>();

        var cars = config.cars;

        int i = 0;
        for (; i < cars.Count; i++)
        {
            var item = cars[i];
            var car = GameObject.Instantiate(item.Agent, geometry.transform);
            ResetController.PlacePrefab(car.transform, geometry, GridStartDistance(i), GridStartOffset(i));
            running.Add(car);
        }
        
        var player = GameObject.Instantiate(config.player.Player, geometry.transform);
        ResetController.PlacePrefab(player.transform, geometry, GridStartDistance(i), GridStartOffset(i));
        running.Add(player);
        raceCamera.Target = player.GetComponent<CamRig>();
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
