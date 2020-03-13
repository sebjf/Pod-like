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

        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileInterval"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileLength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileSpeedStepSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileErrorThreshold"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("AgentInterval"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("AgentPrefab"));

        serializedObject.ApplyModifiedProperties();

        var component = target as ExperienceManager;

        EditorGUILayout.LabelField("Agents Remaining",
            string.Format("{0} Agents",
            component.AgentsRemaining)
            );

        EditorGUILayout.LabelField("Real Time Elapsed",
            string.Format("{0} Min {1} sec",
            (int)(component.elapsedRealTime / 60),
            Mathf.Repeat(component.elapsedRealTime, 60))
            );

        EditorGUILayout.LabelField("Virtual Time Elapsed",
    string.Format("{0} Min {1} sec",
    (int)(component.elapsedVirtualTime / 60),
    Mathf.Repeat(component.elapsedVirtualTime, 60))
    );

        Repaint();
    }
}
