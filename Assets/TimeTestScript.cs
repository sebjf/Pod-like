using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeTestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    float lastTime;

    private void FixedUpdate()
    {
        var time = Time.unscaledTime;
        var diff = time - lastTime;
        lastTime = time;
        Debug.Log(diff);
    }
}
