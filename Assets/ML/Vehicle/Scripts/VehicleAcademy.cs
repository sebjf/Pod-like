using MLAgents;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class VehicleAcademy : Academy
{
    public bool isTraining
    {
        get
        {
            return broadcastHub.broadcastingBrains.Any((Brain brain) => broadcastHub.IsControlled(brain));
        }
    }

    public int maxSteps;

    public GameObject trainingCars;
    public GameObject testCars;

    public override void InitializeAcademy()
    {
        Monitor.SetActive(true);

        if(isTraining)
        {
            foreach (var item in FindObjectsOfType<VehicleAgent>())
            {
                item.agentParameters.maxStep = maxSteps;
            }   
        }

        if(isTraining)
        {
            testCars.SetActive(false);
            trainingCars.SetActive(true);
            GetComponent<RewardAcademy>().enabled = false;
        }
        else
        {
            testCars.SetActive(true);
            trainingCars.SetActive(false);
        }

        // turn off all objects that do not have a brain loaded
        foreach (var agent in FindObjectsOfType<VehicleAgent>())
        {
            if (agent.brain != null)
            {
                if (!broadcastHub.broadcastingBrains.Contains(agent.brain))
                {
                    agent.gameObject.SetActive(false);
                }
            }
        }

        base.InitializeAcademy();
    }



}
