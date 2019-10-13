using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AriadneHeuristic : Decision
{
    float[] actions;

    public override float[] Decide(List<float> vectorObs, List<Texture2D> visualObs, float reward, bool done, List<float> memory)
    {
        if(actions == null)
        {
            actions = new float[2];
        }
        actions[1] = Mathf.Max(1f - vectorObs[2] * 30f,0.15f);

        return actions;
    }

    public override List<float> MakeMemory(List<float> state, List<Texture2D> observation, float reward, bool done, List<float> memory)
    {
        return new List<float>();
    }
}
