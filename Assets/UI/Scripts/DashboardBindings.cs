using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DashboardBindings : MonoBehaviour
{
    public new RaceCamera camera;

    public Text speedometer;
    public Text brakeIndicator;
    public Text sideslip;

    private void Awake()
    {
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
                if(speedometer)
                {
                    speedometer.text = string.Format("{0:0}", vehicle.speed * 2.237);
                }

                if(brakeIndicator)
                {
                    if (vehicle.brake > 0)
                    {
                        brakeIndicator.enabled = true;
                    }
                    else
                    {
                        brakeIndicator.enabled = false;
                    }
                }

                if(sideslip)
                {
                    sideslip.text = string.Format("{0}", Mathf.Rad2Deg * vehicle.sideslipAngle);
                }
            }
        }
    }
}
