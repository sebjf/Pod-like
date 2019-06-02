using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleControllerInput : MonoBehaviour
{
    private Vehicle vehicle;

    private void Awake()
    {
        vehicle = GetComponent<Vehicle>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var throttleinput = Input.GetAxis("Vertical");

        vehicle.throttle = throttleinput;

        vehicle.brake = 0f;

        //automatic braking on deceleration
        if (Mathf.Abs(throttleinput) > 0 && Mathf.Sign(vehicle.speed) != Mathf.Sign(throttleinput) && Mathf.Abs(vehicle.speed) > 1f)
        {
            vehicle.brake = Mathf.Max(vehicle.brake, Mathf.Abs(throttleinput));
        }

        vehicle.brake = Mathf.Max(vehicle.brake, Input.GetKey(KeyCode.X) ? 1f : 0f);

        vehicle.steeringAngle = Input.GetAxisRaw("Horizontal");
    }
}
