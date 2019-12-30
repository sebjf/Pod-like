using MLAgents;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RewardLog : MonoBehaviour
{
    // Start is called before the first frame update

    public string filename;

    private StreamWriter stream;
    private Agent agent;

    private int stepcount;

    private void Awake()
    {
        agent = GetComponent<Agent>();
    }

    void Start()
    {
        if(filename != null && filename != "")
        {
            stream = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(stream != null)
        {
            stream.WriteLine("{0}, {1}, {2}, {3}", stepcount++, Time.fixedTime, agent.GetReward(), agent.GetCumulativeReward());
        }
    }

    private void OnDestroy()
    {
        if (stream != null)
        {
            stream.Close();
        }
    }
}
