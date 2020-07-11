using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class DrivingProfiler : MonoBehaviour
{
    public string directory = @"Support\DrivingProfiles";

    private PathNavigator navigator;
    private Rigidbody body;
    private DerivedPath path;
    private PathObservations observations;
    private Vehicle vehicle;

    [Serializable]
    public class Profile
    {
        public float[] distance;
        public float[] speed;
        public float[] direction;
        public float[] steeringangle;
        public float[] lateralerror;

        public Profile(int length)
        {
            distance = new float[length];
            speed = new float[length];
            direction = new float[length];
            steeringangle = new float[length];
            lateralerror = new float[length];
        }
    }

    private Profile profile;

    public string filename
    {
        get
        {
            return string.Format("{0}.drivingprofile.json", gameObject.name);
        }
    }

    public string fullfile
    {
        get
        {
            return Path.GetFullPath(Path.Combine(directory, filename));
        }
    }


    private void Awake()
    {
        navigator = GetComponent<PathNavigator>();
        body = GetComponent<Rigidbody>();
        observations = GetComponent<PathObservations>();

        if(!observations)
        {
            observations = gameObject.AddComponent<PathObservations>();
        }

        vehicle = GetComponent<Vehicle>();
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

        profile = new Profile(path.waypoints.Count);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        // realistically we will not skip a whole waypoint in a frame, but even if we do, we can interpolate to fix as its easy to detect
        var index = path.WaypointQuery(navigator.Distance).waypoint.index;

        if (body)
        {
            profile.speed[index] = body.velocity.magnitude;
        }
        if(observations)
        {
            profile.direction[index] = observations.directionError;
            profile.lateralerror[index] = observations.understeer + -observations.oversteer;
        }
        profile.steeringangle[index] = vehicle.steeringAngle * vehicle.maxSteerAngle;
    }

    private void OnDestroy()
    {
        if (profile != null)
        {
            for (int i = 0; i < profile.distance.Length; i++)
            {
                profile.distance[i] = path.waypoints[i].Distance;
            }

            Export(directory);
        }
    }

    private void OnApplicationQuit()
    {

    }

    public void Export(string filename)
    {
        using (FileStream stream = new FileStream(fullfile, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(profile));
            }
        }
    }
}
