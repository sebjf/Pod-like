using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(GraphOverlay.SeriesSettings))]
public class SeriesPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var r1 = new Rect(position.x, position.y, position.width / 3, position.height);
        var r2 = r1; r2.x += position.width / 3;
        var r3 = r2; r3.x += position.width / 3;

        EditorGUI.PropertyField(r1, property.FindPropertyRelative("name"), new GUIContent(""));
        EditorGUI.PropertyField(r2, property.FindPropertyRelative("colour"), new GUIContent(""));
        EditorGUI.PropertyField(r3, property.FindPropertyRelative("scale"), new GUIContent(""));

        EditorGUI.EndProperty();
    }
}
