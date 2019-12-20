using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MockAcademy : MonoBehaviour
{
    public int maxSteps;
    public int numActions;

    public Transform agents;

    protected int timesteps;
    protected float[] actions;

    private List<VehicleAgent> activeAgents;

    // Start is called before the first frame update
    void Start()
    {
        actions = new float[numActions];

        activeAgents = new List<VehicleAgent>();

        foreach (var item in FindObjectsOfType<VehicleAgent>())
        {
            if(item.transform.IsChildOf(agents))
            {
                item.gameObject.SetActive(true);
                activeAgents.Add(item);
            }
            else
            {
                item.gameObject.SetActive(false);
            }
        }

        activeAgents.ForEach(a => a.InitializeAgent());
    }

    private void FixedUpdate()
    {
        if (timesteps >= maxSteps)
        {
            activeAgents.ForEach(a => a.Done());
        }

        activeAgents.ForEach(agent =>
        {
            if (agent.IsDone())
            {
                agent.AgentReset();
                agent.ResetReward();
                agent.ClearDone();

                timesteps = 0;
            }
        });

        activeAgents.ForEach(agent => agent.CollectObservations());
        activeAgents.ForEach(agent => agent.AgentAction(actions, ""));

        timesteps++;
    }
}
