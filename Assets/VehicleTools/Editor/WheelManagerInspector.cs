﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WheelManager))]
public class WheelManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorUtility.SetDirty((target as WheelManager));
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("wheelAssignments"), true);
        serializedObject.ApplyModifiedProperties();
    }
}