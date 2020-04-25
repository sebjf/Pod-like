using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class PathFinderPlayer : MonoBehaviour
{
    public string profileFilename;

    private Navigator navigator;
    private Rigidbody body;
    private DerivedPath path;
    private Autopilot autopilot;

    private float[] profile;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
        autopilot = GetComponent<Autopilot>();
    }

    // Start is called before the first frame update
    void Start()
    {
        path = navigator.waypoints as DerivedPath;

        if(path == null)
        {
            this.enabled = false;
            return;
        }

        profile = Import(profileFilename).ToArray();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        autopilot.speed = profile[path.WaypointQuery(navigator.TrackDistance).waypoint.index];
    }

    protected static int mod(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    public IEnumerable<float> Import(string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Open))
        {
            using (StreamReader reader = new StreamReader(stream))
            {
                while(!reader.EndOfStream)
                {
                    yield return float.Parse(reader.ReadLine());
                }
            }
        }
    }
}
