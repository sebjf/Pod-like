using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Circuit))]
public class CircuitInspector : Editor {

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("GridPositions"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("StartDirection"));

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void OnSceneGUI()
    {
        Circuit circuit = (Circuit)target;

        for (int i = 0; i < circuit.GridPositions.Length; i++)
        {
            EditorGUI.BeginChangeCheck();
            var newposition = Handles.PositionHandle(circuit.GridPositions[i], Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(circuit, "Change Starting Position");
                circuit.GridPositions[i] = newposition;

            }
        }        
    }


}
