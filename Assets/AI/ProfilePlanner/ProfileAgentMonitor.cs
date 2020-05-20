using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProfileAgentMonitor : MonoBehaviour
{
    void Update()
    {
#if UNITY_EDITOR
        var activateGameObject = UnityEditor.Selection.activeGameObject;
        if (activateGameObject)
        {
            if (activateGameObject.activeInHierarchy)
            {
                var pathFinder = UnityEditor.Selection.activeGameObject.GetComponentInChildren<ProfileAgent>();
                if (pathFinder)
                {
                    GraphOverlay.Plot("Profile", pathFinder.profile.Select(x => x.speed));
                    GraphOverlay.Plot("Speed", pathFinder.profile.Select(x => x.actual));
                    GraphOverlay.Plot("Traction", pathFinder.profile.Select(x => x.traction ? 1f : 0f));
                    GraphOverlay.Plot("Error", pathFinder.profile.Select(x => x.error));
                    GraphOverlay.Plot("Drift", pathFinder.profile.Select(x => x.sideslip));
                }
            }
        }
#endif
    }
}
