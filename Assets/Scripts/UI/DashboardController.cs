using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashboardController : MonoBehaviour
{
    Text m_SpeedText;
    DriftCamera cameraController;

    private void Awake()
    {
        m_SpeedText = GetComponentInChildren<Text>();
        cameraController = GetComponentInParent<DriftCamera>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(cameraController.cameraRig != null)
        {
            var vehicle = cameraController.cameraRig.GetComponent<Vehicle>();
            if (vehicle)
            {
                m_SpeedText.text = string.Format("Speed: {0:0.00} m/s, {1:0} mph", vehicle.speed, vehicle.speed * 2.237);
            }
        }
    }
}
