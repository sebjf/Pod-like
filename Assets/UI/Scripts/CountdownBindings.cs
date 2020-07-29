using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Channels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CountdownBindings : MonoBehaviour
{
    public RaceManager raceManager;
    public AudioClip[] audioClips;

    private AudioSource audioSource;
    private Text text;
    private Util.Trigger<int> countdownTrigger;


    private void Awake()
    {
        text = GetComponent<Text>();
        audioSource = GetComponent<AudioSource>();
        countdownTrigger = new Util.Trigger<int>();
        countdownTrigger.OnChanged.AddListener(CountdownChanged);
    }

    // Update is called once per frame
    void Update()
    {
        switch (raceManager.stage)
        {
            case RaceStage.Countdown:
                text.text = raceManager.countdown.ToString();
                break;
            case RaceStage.Preparation:
                text.text = "Ready";
                break;
            case RaceStage.Race:
                if(raceManager.raceTime > 0.75f)
                {
                    text.text = "";
                }
                else
                {
                    text.text = "Go!";
                }
                break;
            case RaceStage.Finish:
                text.text = "Race Over!";
                break;
        }

        countdownTrigger.Update(raceManager.countdown);
    }

    void CountdownChanged()
    {
        audioSource.PlayOneShot(audioClips[raceManager.countdown]);
    }
}
