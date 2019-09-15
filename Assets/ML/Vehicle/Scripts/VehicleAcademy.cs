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

    public GameObject trainingCars;
    public GameObject testCars;

    public override void InitializeAcademy()
    {
        Monitor.SetActive(true);

        if(!isTraining)
        {
            // use reflection so we can more easily update the version of ml-agents
            typeof(Academy).GetField("m_MaxSteps", System.Reflection.BindingFlags.NonPublic).SetValue(this, 0);
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
    }



}
