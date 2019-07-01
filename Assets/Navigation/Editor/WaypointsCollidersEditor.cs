using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WaypointsColliders))]
public class WaypointsCollidersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var component = target as WaypointsColliders;

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
