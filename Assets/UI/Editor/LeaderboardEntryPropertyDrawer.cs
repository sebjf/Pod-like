using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LeaderboardEntry))]
public class LeaderboardEntryPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Using BeginProperty / EndProperty on the parent property means that
        // prefab override logic works on the entire property.
        EditorGUI.BeginProperty(position, label, property);

        // Draw label
        position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

        // Don't make child fields be indented
        var indent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;

        // Draw fields - passs GUIContent.none to each so they are drawn without labels
        EditorGUI.PropertyField(new Rect(position.x + 0, position.y, 60, position.height), property.FindPropertyRelative("driver"), GUIContent.none);
        EditorGUI.PropertyField(new Rect(position.x + 60, position.y, 60, position.height), property.FindPropertyRelative("car"), GUIContent.none);
        EditorGUI.PropertyField(new Rect(position.x + 120, position.y, 35, position.height), property.FindPropertyRelative("interval"), GUIContent.none);
        EditorGUI.PropertyField(new Rect(position.x + 155, position.y, 35, position.height), property.FindPropertyRelative("reaction"), GUIContent.none);

        // Set indent back to what it was
        EditorGUI.indentLevel = indent;

        EditorGUI.EndProperty();
    }
}