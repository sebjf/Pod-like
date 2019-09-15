using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardAcademy : MonoBehaviour
{
    public VehicleAgent agent;

    public int maxSteps;
    public int numActions;

    protected int timesteps;
    protected float[] actions;

    // Start is called before the first frame update
    void Start()
    {
        actions = new float[numActions];
        agent.InitializeAgent();
    }

    private void FixedUpdate()
    {
        if (timesteps >= maxSteps)
        {
            agent.Done();
        }

        if(agent.IsDone())
        {
            Debug.Log(agent.TotalDistanceTravelled + ", " + agent.GetCumulativeReward());
        }

        if(agent.IsDone())        
        {
            agent.AgentReset();
            agent.ResetReward();
            agent.ClearDone();

            timesteps = 0;
        }

        agent.CollectObservations();
        agent.AgentAction(actions, "");

        timesteps++;
    }
}
