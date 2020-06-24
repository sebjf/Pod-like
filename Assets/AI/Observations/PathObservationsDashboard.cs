using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PathObservationsDashboard : MonoBehaviour
{
    public Text directionError;

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
                    directionError.text = "Direction Error: " + observations.directionError.ToString();
                }
            }
        }
#endif
    }
}
