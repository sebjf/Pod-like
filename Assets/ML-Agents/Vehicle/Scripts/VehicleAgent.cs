using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MLAgents;

public class VehicleAgent : Agent
{
    private Vehicle vehicle;
    private Rigidbody body;

    private Vector3 startposition;
    private Quaternion startrotation;

    public GameObject road;
    private Waypoint[] waypoints;

    private class TrackSegment
    {
        public Waypoint wp1;
        public Waypoint wp2;
    }

    private TrackSegment s;

    public bool logToConsole = false;

    public override void InitializeAgent()
    {
        vehicle = GetComponent<Vehicle>();
        body = GetComponent<Rigidbody>();

        startposition = transform.position;
        startrotation = transform.rotation;

        GetComponent<VehicleControllerInput>().enabled = false;

        waypoints = road.GetComponentsInChildren<Waypoint>().ToArray();
    }

    public override void CollectObservations()
    {
        AddVectorObs(Vector3.Dot(body.velocity, transform.forward) * 0.01f);
        AddVectorObs(transform.InverseTransformPoint(waypoints.Last().transform.position).normalized);
        
        //AddVectorObs(transform.InverseTransformPoint(s.wp1.transform.position).normalized);
        //AddVectorObs(s.wp1.width);
        AddVectorObs(cross);
        //AddVectorObs(transform.InverseTransformPoint(s.wp2.transform.position).normalized);
        //AddVectorObs(s.wp2.width);
    }

    private void UpdateSegment()
    {
        if(waypoints == null || waypoints.Length <= 0)
        {
            waypoints = road.GetComponentsInChildren<Waypoint>().ToArray();
        }

        if(s == null)
        {
            s = new TrackSegment();
        }

        if(s.wp1 == null)
        {
            s.wp1 = waypoints[0];
        }
        
        if(s.wp2 == null)
        {
            s.wp2 = waypoints[1];
        }

        foreach (var waypoint in waypoints)
        {
            if ((waypoint.transform.position - transform.position).magnitude < (s.wp1.transform.position - transform.position).magnitude)
            {
                s.wp1 = waypoint;

                if (s.wp1.next != null)
                {
                    s.wp2 = waypoint.next;
                }
                else
                {
                    s.wp2 = s.wp1;
                }
            }
        }

        var t = Vector3.Dot((s.wp2.transform.position - s.wp1.transform.position).normalized, (transform.position - s.wp1.transform.position));
        p = s.wp1.transform.position + s.wp1.normal * t;
        var tn = t / (s.wp2.transform.position - s.wp1.transform.position).magnitude;
        width = Mathf.Lerp(s.wp1.width, s.wp2.width, tn);
        tangent = Vector3.Lerp(s.wp1.tangent, s.wp2.tangent, tn);
        cross = (transform.position - p).magnitude / width * Vector3.Dot((transform.position - p).normalized, s.wp1.tangent);
    }

    float previousDistance;
    float previousDistanceReduction;
    int i;

    Vector3 p;
    float width;
    Vector3 tangent;
    float cross;

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        vehicle.throttle = Mathf.Clamp(vectorAction[0], -1, 1);
        vehicle.steeringAngle = Mathf.Clamp(vectorAction[1], -1, 1);

        UpdateSegment();

        /*
        var orientation = Vector3.Dot(transform.forward, s.wp1.normal);
        if(orientation < 0)
        {
            AddReward(-0.25f);
        }
        else
        {
            AddReward(0.25f);
        }
        */
        
        var forwardspeed = Vector3.Dot(body.angularVelocity, transform.forward);
        if(forwardspeed < 0)
        {
            AddReward(-0.05f);
        }
        else
        {
            AddReward(0.025f);
            AddReward(forwardspeed * 0.01f);
        }
        
        
        if(Mathf.Abs(cross) < 0.8f)
        {
            AddReward(0.05f);
        }
        else
        {
            AddReward(-0.05f);
        }

        var distance = (waypoints.Last().transform.position - transform.position).magnitude;
        var distanceReduction = previousDistance - distance;
        previousDistance = distance;
        if(distanceReduction > 0)
        {
            AddReward(0.05f);
        }
        else
        {
            AddReward(-0.01f);
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
        previousDistance = (waypoints.Last().transform.position - transform.position).magnitude;
    }

    private void OnDrawGizmos()
    {
        UpdateSegment();
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(s.wp1.transform.position, 0.5f);
        Gizmos.DrawWireSphere(s.wp2.transform.position, 0.5f);

        Gizmos.color = Color.red;
        
        Gizmos.DrawWireSphere(p, 0.25f);
        Gizmos.DrawLine(p, p + tangent * width);
        Gizmos.DrawLine(p, p - tangent * width);
    }
}
