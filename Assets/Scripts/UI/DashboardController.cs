using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashboardController : MonoBehaviour
{
    Text m_SpeedText;
    Text brakingText;
    Text driftText;
    DriftCamera cameraController;

    private void Awake()
    {
        m_SpeedText = transform.Find("Speed").GetComponent<Text>();
        brakingText = transform.Find("Braking").GetComponent<Text>();
        driftText = transform.Find("Drift").GetComponent<Text>();
        cameraController = GetComponentInParent<DriftCamera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(cameraController.Target != null)
        {
            var vehicle = cameraController.Target.GetComponent<Vehicle>();
            if (vehicle)
            {
                m_SpeedText.text = string.Format("Speed: {0:0.00} m/s, {1:0} mph", vehicle.speed, vehicle.speed * 2.237);

                if(vehicle.brake > 0)
                {
                    brakingText.enabled = true;
                }
                else
                {
                    brakingText.enabled = false;
                }
            }

            var pathfinderagent = cameraController.Target.GetComponent<PathDriverAgent>();
            if(pathfinderagent)
            {
                driftText.text = string.Format("Drift: {0}", Mathf.Rad2Deg * pathfinderagent.sideslip);
            }
        }
    }
}
