using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Vehicle))]
public class PathController : MonoBehaviour
{
    private PathNavigator navigator;

    private void Awake()
    {
        navigator = GetComponent<PathNavigator>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(navigator.waypoints == null)
        {
            var path = gameObject.AddComponent<InterpolatedPath>();
            path.coefficient = Random.value;
            path.Initialise();
            navigator.waypoints = path;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
