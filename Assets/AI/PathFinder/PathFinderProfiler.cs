using System.IO;
using System.Linq;
using UnityEngine;

public class PathFinderProfiler : MonoBehaviour
{
    public string profileFilename;

    private Navigator navigator;
    private Rigidbody body;
    private DerivedPath path;

    private float[] profile;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        body = GetComponent<Rigidbody>();
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

        profile = new float[path.waypoints.Count * 2];
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // realistically we will not skip a whole waypoint in a frame, but even if we do, we can interpolate to fix as its easy to detect
        var index = path.WaypointQuery(navigator.TrackDistance).waypoint.index;
        profile[(index * 2) + 0] = navigator.TrackDistance;
        //profile[(index * 2) + 1] = body.GetComponent<Vehicle>().speed;
        profile[(index * 2) + 1] = body.velocity.magnitude;
    }

    protected static int mod(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    private void OnApplicationQuit()
    {
        if (profile != null)
        {
            Export(profileFilename);
        }
    }

    public void Export(string filename)
    {
        using (FileStream stream = new FileStream(filename, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                foreach (var item in profile)
                {
                    writer.WriteLine(item);
                }
            }
        }
    }
}
