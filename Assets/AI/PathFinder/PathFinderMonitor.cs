using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinderMonitor : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        var activateGameObject = UnityEditor.Selection.activeGameObject;
        if (activateGameObject)
        {
            if (activateGameObject.activeInHierarchy)
            {
                var pathFinder = UnityEditor.Selection.activeGameObject.GetComponentInChildren<PathFinder>();
                if (pathFinder)
                {
                    GraphOverlay.Plot("Profile", pathFinder.profile.Select(x => x.speed));
                    GraphOverlay.Plot("Speed", pathFinder.profile.Select(x => x.actual));
                    GraphOverlay.Plot("Traction", pathFinder.profile.Select(x => x.traction ? 1f : 0f));
                    GraphOverlay.Plot("Error", pathFinder.profile.Select(x => x.error));
                }
            }
        }
#endif
    }
}
