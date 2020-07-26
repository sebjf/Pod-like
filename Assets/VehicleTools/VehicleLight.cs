using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VehicleLight : MonoBehaviour
{
    private Vehicle vehicle;
    private new Light light;
    private float intensity;

    private void Awake()
    {
        vehicle = GetComponentInParent<Vehicle>();
        light = GetComponent<Light>();
        intensity = light.intensity;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        light.intensity = intensity * (vehicle.brake > 0.01 ? 1 : 0);
    }
}
