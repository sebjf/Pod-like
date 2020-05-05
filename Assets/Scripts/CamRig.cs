using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRig : MonoBehaviour
{
    public int priority = 0;
    public Transform lookAtTarget;
    public Transform positionTarget;
    public Transform sideView;

    private void Reset()
    {
        lookAtTarget = transform.GetChild(0);
        positionTarget = transform.GetChild(1);
        sideView = transform.GetChild(2);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
