using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathMetadata))]
public class PathMetadataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Resolution"));
        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Generate"))
        {
            Undo.RecordObject(target, "Generate Metadata");
            (target as PathMetadata).Generate();
        }

        if (GUILayout.Button("Export"))
        {
            var filename = EditorUtility.SaveFilePanel("Save Path", "", "", "txt");
            if (filename.Length != 0)
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filename))
                {
                    foreach (var item in (target as PathMetadata).nodes)
                    {
                        writer.WriteLine(item.i);
                    }
                }
            }
        }

    }
}
