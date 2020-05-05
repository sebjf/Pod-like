using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrackObservationsMonitor : MonoBehaviour
{
    public int numObservations = 25;
    public float pathInterval = 5;

    [HideInInspector]
    public float[] Curvature;

    [HideInInspector]
    public float[] Camber;

    [HideInInspector]
    public float[] Inclination;

    [HideInInspector]
    public float[] Distance;


#if UNITY_EDITOR
    private void FixedUpdate()
    {
        var activateGameObject = UnityEditor.Selection.activeGameObject;
        if (activateGameObject)
        {
            if (activateGameObject.activeInHierarchy)
            {
                var navigator = UnityEditor.Selection.activeGameObject.GetComponent<Navigator>();
                if (navigator)
                {
                    if (Curvature.Length != numObservations)
                    {
                        Curvature = new float[numObservations];
                        Camber = new float[numObservations];
                        Inclination = new float[numObservations];
                        Distance = new float[numObservations];
                    }

                    for (int i = 0; i < numObservations; i++)
                    {
                        var d = navigator.TrackDistance + i * pathInterval;
                        var q = navigator.waypoints.Query(d);
                        Camber[i] = q.Camber;
                        Curvature[i] = q.Curvature;
                        Inclination[i] = q.Inclination;
                        Distance[i] = d;
                    }

                    GraphOverlay.Plot("Curvature", Curvature);
                    GraphOverlay.Plot("Camber", Camber);
                    GraphOverlay.Plot("Inclination", Inclination);
                }
            }
        }
    }
#endif

#if UNITY_EDITOR
    protected void OnDrawGizmos()
    {
        var activateGameObject = UnityEditor.Selection.activeGameObject;
        if (activateGameObject)
        {
            if (activateGameObject.activeInHierarchy)
            {
                var navigator = UnityEditor.Selection.activeGameObject.GetComponent<Navigator>();
                if (navigator)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 0; i < numObservations; i++)
                    {
                        Gizmos.DrawWireSphere(navigator.waypoints.Query(navigator.TrackDistance + i * pathInterval).Midpoint, 0.25f);
                    }
                }
            }
        }
    }
#endif
}
