using MLAgents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleAgent : Agent
{
    private Vehicle vehicle;
    private Rigidbody body;

    private Vector3 startposition;
    private Quaternion startrotation;

    public Transform target;

    public bool logToConsole = false;

    public override void InitializeAgent()
    {
        vehicle = GetComponent<Vehicle>();
        body = GetComponent<Rigidbody>();

        startposition = transform.position;
        startrotation = transform.rotation;

        previousDistance = (target.position - transform.position).magnitude;

        GetComponent<VehicleControllerInput>().enabled = false;
    }

    public override void CollectObservations()
    {
        AddVectorObs(Vector3.Dot(body.velocity, transform.forward) * 0.01f);

        var relativeTargetPosition = transform.InverseTransformPoint(target.position).normalized;
        AddVectorObs(relativeTargetPosition);
    }

    float previousDistance;
    float previousDistanceReduction;
    int i;

    public override void AgentAction(float[] vectorAction, string textAction)
    {
        vehicle.throttle = Mathf.Clamp(vectorAction[0], -1, 1);
        vehicle.steeringAngle = Mathf.Clamp(vectorAction[1], -1, 1);

        float distance = (target.position - transform.position).magnitude;
        
        /*
        if (i > 10)
        {
            float distanceReduction = previousDistance - distance;
            previousDistance = distance;
            
            if(distanceReduction > 0)
            {
                AddReward(0.2f);
                AddReward(distanceReduction * 0.01f);
                AddReward(Vector3.Dot(body.velocity, transform.forward) * 0.01f);
                AddReward(Vector3.Dot((target.position - transform.position).normalized, transform.forward) * 0.01f);
            }
            if(distanceReduction < 0)
            {
                SetReward(-0.2f);
            }

            i = 0;
        }
        i++;
        */
        
        
        float distanceReduction = previousDistance - distance;
        previousDistance = distance;

        if (distanceReduction > 0)
        {
            AddReward(1f);
            AddReward(distanceReduction * 0.01f);
            AddReward(Vector3.Dot(body.velocity, transform.forward) * 0.01f);
            AddReward(Vector3.Dot((target.position - transform.position).normalized, transform.forward) * 0.01f);
        }
        else
        {
            SetReward(-1f);
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
        previousDistance = (target.position - transform.position).magnitude;
    }
}
