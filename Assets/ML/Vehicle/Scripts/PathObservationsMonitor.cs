using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathObservationsMonitor : MonoBehaviour
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
                var observations = UnityEditor.Selection.activeGameObject.GetComponentInChildren<PathObservations>();
                if (observations)
                {
                    /*
                    GraphOverlay.Plot("Profile", observations..Select(x => x.speed));
                    GraphOverlay.Plot("Speed", observations.profile.Select(x => x.actual));
                    GraphOverlay.Plot("Traction", observations.profile.Select(x => x.traction ? 1f : 0f));
                    GraphOverlay.Plot("Error", observations.profile.Select(x => x.error));
                    GraphOverlay.Plot("Drift", observations.profile.Select(x => x.drift));
                    */
                }
            }
        }
#endif
    }
}