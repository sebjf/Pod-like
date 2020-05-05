using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinderVolume : MonoBehaviour
{
    private new BoxCollider collider;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
    }

    public void AdjustError(PathFinder agent, ref float error)
    {
        if(collider.bounds.Contains(agent.transform.position))
        {
            error = 0;
        }
    }

    public bool Excludes(Vector3 point)
    {
        return (collider.bounds.Contains(point));
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
