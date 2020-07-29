using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAudioController : MonoBehaviour
{
    private AudioSource audioSource;
    private Drivetrain driveTrain;
    private Vehicle vehicle;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        vehicle = GetComponentInParent<Vehicle>();
        driveTrain = GetComponentInParent<Drivetrain>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        audioSource.pitch = 1f + driveTrain.EvaluateEngineRange(vehicle.rpm) * 11f;
    }
}
