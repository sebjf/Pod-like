using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class TrainingProperties
{
    public static string AgentLayer = "Training Car";
}

public class AgentManagerComplete : UnityEvent<IAgentManager> 
{
}

public interface IAgentManager
{
    void SetAgentPrefab(GameObject prefab);

    IEnumerable<Transform> Agents { get; }
    AgentManagerComplete OnComplete { get; }
    
    /// <summary>
    /// A string describing the prefab, circuit and agent combination that this manager is running. The string must be a valid filename.
    /// </summary>
    string Filename { get; }

    /// <summary>
    /// The experience dataset serialised as a json string.
    /// </summary>
    string ExportJson();
}
