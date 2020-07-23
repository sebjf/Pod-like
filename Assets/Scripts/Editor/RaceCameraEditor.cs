using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(RaceCamera))]
public class RaceCameraEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var component = target as RaceCamera;

        if (component.cameraRigs != null) {
            for (int i = 0; i < Mathf.Min(component.cameraRigs.Length, 10); i++)
            {
                if(GUILayout.Button(component.cameraRigs[i].name))
                {
                    component.Target = component.cameraRigs[i];
                }
            }
        }
    }
}
