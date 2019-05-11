using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerInput : MonoBehaviour
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
        vehicle.throttle = Input.GetAxis("Vertical");
        vehicle.brake = Input.GetKey(KeyCode.X);
        vehicle.steeringAngle = Input.GetAxisRaw("Horizontal");
    }
}
