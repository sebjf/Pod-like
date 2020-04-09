using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(Navigator))]
public class NavigatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var paths = new Dictionary<string, TrackPath>();
        foreach (var item in (target as Navigator).GetComponentsInParent<TrackPath>())
        {
            paths.Add(TrackPathName(item), item);
        }
        var pathNames = paths.Keys.ToList();
        var selected = serializedObject.FindProperty("waypoints").objectReferenceValue as TrackPath;
        var selectedIndex = pathNames.IndexOf(TrackPathName(selected));
        var newSelectedIndex = EditorGUILayout.Popup(selectedIndex, pathNames.ToArray());
        if(selectedIndex != newSelectedIndex)
        {
            serializedObject.FindProperty("waypoints").objectReferenceValue = paths[pathNames[newSelectedIndex]];

        }

        serializedObject.ApplyModifiedProperties();

        var navigator = target as Navigator;

        EditorGUILayout.LabelField("Distance", navigator.TrackDistance.ToString());

    }

    private static string TrackPathName(TrackPath item)
    {
        if(item == null)
        {
            return null;
        }
        return item.name + " " + item.GetType().Name;
    }

}
