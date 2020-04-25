using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrackObservations))]
public class TrackObservationsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("numObservations"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathInterval"));
        serializedObject.ApplyModifiedProperties();

        var observations = target as TrackObservations;

        if(GUILayout.Button("Export"))
        {
            var filename = EditorUtility.SaveFilePanel("Save Observations", "", "", "txt");
            if (filename.Length != 0)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filename))
                {
                    for (int i = 0; i < observations.numObservations; i++)
                    {
                        writer.WriteLine(observations.Distance[i]);
                        writer.WriteLine(observations.Curvature[i]);
                    }
                }
            }
        }
    }
}
