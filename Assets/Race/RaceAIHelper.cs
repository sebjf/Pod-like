using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;

public class RaceAIHelper : MonoBehaviour
{
    private void Awake()
    {
        agents = new List<Agent>();
    }

    private RaceManager manager;

    public void OnRacePrepared(RaceManager manager)
    {
        this.manager = manager;
        agents = manager.competitors.Select(g => g.GetComponent<Agent>()).Where(a => a != null).ToList();
        difficulty = manager.configuration.difficulty;
    }

    private float difficulty;
    private List<Agent> agents;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var agent in agents)
        {
            agent.Difficulty = difficulty;
            agent.state = manager.state;
        }
    }
}
