using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeController : MonoBehaviour
{
    public bool enableCapture;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(enableCapture)
        {
            Time.captureFramerate = 60;
        }
        else
        {
            Time.captureFramerate = 0;
        }
    }
}
