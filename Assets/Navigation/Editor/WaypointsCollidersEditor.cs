using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrackGeometryColliders))]
public class WaypointsCollidersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var component = target as TrackGeometryColliders;

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("height"));

        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Build"))
        {
            Undo.RecordObject(target, "Rebuild Waypoints Colliders");
            component.Rebuild();
        }
    }
}
