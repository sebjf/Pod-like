using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Car))]
public class CarInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var component = target as Car;

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("carInfo"));

        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Import"))
        {
            component.Import();
        }

    }

    protected virtual void OnSceneGUI()
    {
        Car car = (Car)target;
    }
}
