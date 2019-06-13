using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;

public class VehicleAgent : Agent
{
    private Vehicle vehicle;
    private Rigidbody body;

    private Waypoints waypoints;

    private Vector3 startposition;
    private Quaternion startrotation;

    public bool logToConsole = false;

    public override void InitializeAgent()
    {
        vehicle = GetComponent<Vehicle>();
        body = GetComponent<Rigidbody>();
        waypoints = GameObject.FindObjectOfType<Waypoints>();

        startposition = transform.position;
        startrotation = transform.rotation;

        GetComponent<VehicleControllerInput>().enabled = false;

        previousDistance = 0f;
        totalDistance = 0f;
        startTime = Time.time;

        observations = new Observation[10];
    }

    public override void CollectObservations()
    {
        AddVectorObs(transform.InverseTransformVector(body.velocity) * 0.1f);

        for (int i = 0; i < 10; i++)
        {
            // the waypoints 20 m ahead in 2 m increments along the centreline, normalised by 20 m
            var sampleposition = waypoints.Evaluate(body.position) + (i * 10);
            var mp = waypoints.Midline(sampleposition);
            var w = waypoints.Width(sampleposition);
            var t = waypoints.Tangent(sampleposition);

            observations[i].left = mp - t * w * 0.5f;
            observations[i].right = mp + t * w * 0.5f;

            AddVectorObs(transform.InverseTransformPoint(observations[i].left)  / 100f);
            AddVectorObs(transform.InverseTransformPoint(observations[i].right) / 100f);
        }
    }

    public struct Observation
    {
        public Vector3 left;
        public Vector3 right;
        public float width;
    }

    Observation[] observations;

    float distance;
    float previousDistance;
    float distanceTravelled;
    float averageDistancePerTime;
    float totalDistance;
    float forwardspeed;
    float startTime;
    float angle;
    float margin;
    
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        var throttle = Mathf.Clamp(vectorAction[0], -1, 1);
        if(throttle < 0)
        {
            vehicle.brake = Mathf.Abs(throttle);
            vehicle.throttle = 0f;
        }
        else
        {
            vehicle.brake = 0f;
            vehicle.throttle = throttle;
        }

        vehicle.steeringAngle = Mathf.Clamp(vectorAction[1], -1, 1);

        distance = waypoints.Evaluate(body.position);
        distanceTravelled = distance - previousDistance;

        if(distanceTravelled < 0 && Mathf.Abs(distanceTravelled) > (waypoints.totalLength / 2)) // we have crossed over the finish line
        {
            //distanceTravelled = (waypoints.totalLength - previousDistance) + distance;
            distanceTravelled = 1f;
        }

        previousDistance = distance;

        if(distanceTravelled < 0.01)
        {
            AddReward(-1f);
        }
        if(distanceTravelled > 0)
        {
            AddReward(0.5f);
            AddReward(distanceTravelled);
        }

        totalDistance += distanceTravelled;
        averageDistancePerTime = totalDistance / ((Time.time - startTime) + Mathf.Epsilon);
        if (!float.IsNaN(averageDistancePerTime))
        {
            //AddReward(averageDistancePerTime * 0.01f);
        }

        angle = Vector3.Dot(waypoints.Normal(distance), transform.forward);
        if (angle < 0)
        {
            AddReward(-1f);
        }

        var mp = waypoints.Midline(distance);
        var width = waypoints.Width(distance);
        margin = (width / 2) - (body.position - mp).magnitude;
        if (margin < 0)
        {
            AddReward(-10f);
        }

        if (logToConsole)
        {
            Monitor.Log(gameObject.name, vectorAction);
        }        
    }

    public override void AgentReset()
    {
        transform.position = startposition;
        transform.rotation = startrotation;
        body.velocity = Vector3.zero;
        previousDistance = 0f;
        totalDistance = 0f;
        startTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.BeginGUI();
        GUIStyle style = new GUIStyle();

        string content = "";
        content += "Distance: " + distance + "\n";
        content += "distanceTravelled: " + distanceTravelled + "\n";
        content += "angle: " + angle + "\n";
        content += "margin: " + margin + "\n";

        UnityEditor.Handles.Label(transform.position, content, style);

        UnityEditor.Handles.EndGUI();

        if (observations != null)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < observations.Length; i++)
            {
                Gizmos.DrawWireSphere(observations[i].left, 1f);
                Gizmos.DrawWireSphere(observations[i].right, 1f);
            }
        }
    }
}
