using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DerivedPath),true)]
public class DerivedPathEditor : Editor
{
    private int Steps = 100;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Resolution"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Barrier"));

        if(target is CenterlinePath)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("curvature"));
        }

        serializedObject.ApplyModifiedProperties();

        Steps = EditorGUILayout.IntField("Steps", Steps);

        DerivedPath path = (target as DerivedPath);

        if (GUILayout.Button("Generate"))
        {
            Undo.RecordObject(path, "Reinitialise Path");
            path.Initialise();
        }

        if (GUILayout.Button("Fit"))
        {
            Undo.RecordObject(path, "Optimise Path");
            path.Step(Steps);
        }

        if (GUILayout.Button("Step"))
        {
            path.Step(1);
        }

        EditorGUILayout.LabelField("Waypoints", path.waypoints.Count.ToString());
        EditorGUILayout.LabelField("Length", path.totalLength.ToString());
    }
}
