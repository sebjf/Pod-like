using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashboardController : MonoBehaviour
{
    public new RaceCamera camera;

    Text m_SpeedText;
    Text brakingText;
    Text sideslipText;

    private void Awake()
    {
        m_SpeedText = transform.Find("Speed").GetComponent<Text>();
        brakingText = transform.Find("Braking").GetComponent<Text>();
        sideslipText = transform.Find("Sideslip").GetComponent<Text>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(camera.Target != null)
        {
            var vehicle = camera.Target.GetComponent<Vehicle>();
            if (vehicle)
            {
                m_SpeedText.text = string.Format("Speed: {0:0.00} m/s, {1:0} mph RPM: {2}", vehicle.speed, vehicle.speed * 2.237, vehicle.rpm);

                if(vehicle.brake > 0)
                {
                    brakingText.enabled = true;
                }
                else
                {
                    brakingText.enabled = false;
                }

                sideslipText.text = string.Format("Sideslip Angle: {0}", Mathf.Rad2Deg * vehicle.sideslipAngle);
            }
        }
    }
}
