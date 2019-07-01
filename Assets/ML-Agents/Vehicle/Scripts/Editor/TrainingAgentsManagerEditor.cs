using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TrainingAgentsManager))]
public class TrainingAgentsManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var component = target as TrainingAgentsManager;

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("numCars"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("carPrefab"));
        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Place Cars"))
        {
            component.PlaceCars();
        }
    }
}
