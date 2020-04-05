using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Navigator))]
public class NavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("waypoints"));
        serializedObject.ApplyModifiedProperties();

        var navigator = target as Navigator;

        EditorGUILayout.LabelField("Distance", navigator.TrackDistance.ToString());

    }
}
