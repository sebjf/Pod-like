using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathFinderManager))]
public class PathFinderManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var component = target as PathFinderManager;

        serializedObject.Update();

        var profileInterval = -1f;
        try
        {
            profileInterval = component.AgentPrefab.GetComponentInChildren<PathDriverAgent>().observationsInterval;
        }
        catch
        {
            // something isnt set up right.
        }
        EditorGUILayout.LabelField("Profile Interval", profileInterval.ToString());

        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileLength"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileSpeedStepSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("profileErrorThreshold"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("AgentInterval"));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("AgentPrefab"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("directory"));

        serializedObject.ApplyModifiedProperties();

        EditorStyles.label.wordWrap = true;
        EditorGUILayout.LabelField("File", component.filename);

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
