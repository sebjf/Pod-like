using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GeometryFilter))]
public class GeometryFilterInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"));
        serializedObject.ApplyModifiedProperties();

        
    }

    private void OnSceneGUI()
    {
        

    }
}
