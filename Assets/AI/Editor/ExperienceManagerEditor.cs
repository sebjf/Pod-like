using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ExperienceManager))]
public class ExperienceManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("SpeedMin"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SpeedMax"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("SpeedStep"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("StepTime"));

        serializedObject.ApplyModifiedProperties();

        var component = target as ExperienceManager;

        EditorGUILayout.LabelField("Time Remaining",
            string.Format("{0} seconds ({1} min)",
            component.TimeRemaining,
            component.TimeRemaining / 60)
            );
    }
}
