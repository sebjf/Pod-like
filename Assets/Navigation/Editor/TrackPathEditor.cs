using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrackPath),true)]
public class TrackPathEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Resolution"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Barrier"));
        serializedObject.ApplyModifiedProperties();

        TrackPath path = (target as TrackPath);

        if (GUILayout.Button("Generate"))
        {
            Undo.RecordObject(path, "Reinitialise Path");
            path.Initialise();
        }

        if (GUILayout.Button("Fit"))
        {
            Undo.RecordObject(path, "Optimise Path");
            path.Step(100);
        }

        if (GUILayout.Button("Step"))
        {
            path.Step(1);
        }
    }
}
