using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(DerivedPath),true)]
public class DerivedPathEditor : Editor
{
    private int Steps = 100;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("Resolution"));

        if (target is InterpolatedPath)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("coefficient"));
        }

        if (target is NamedPath)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Name"));
        }

        serializedObject.ApplyModifiedProperties();

        DerivedPath path = (target as DerivedPath);

        if (GUILayout.Button("Generate"))
        {
            Undo.RecordObject(path, "Reinitialise Path");
            path.Initialise();
        }

        EditorGUI.BeginDisabledGroup(path.waypoints.Count <= 0); 

        if (GUILayout.Button("Export Sections"))
        {
            ExportSections(path.GetSections());
        }

        if (GUILayout.Button("Export Observations"))
        {
            ExportObservations(path);
        }

        if (GUILayout.Button("Load"))
        {
            Undo.RecordObject(path, "Load Path");
            path.Load(ImportWeights());
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.LabelField("Waypoints", path.waypoints.Count.ToString());
        EditorGUILayout.LabelField("Length", path.totalLength.ToString());

        if(target is InterpolatedPath)
        {
            var ip = (target as InterpolatedPath);
            if (ip.crossoverPoints != null)
            {
                EditorGUILayout.LabelField("Crossovers", ip.crossoverPoints.Count.ToString());
            }
        }
    }

    private void ExportSections(IEnumerable<TrackSection> sections)
    {
        var filename = EditorUtility.SaveFilePanel("Save Path", System.IO.Path.GetDirectoryName(SceneManager.GetActiveScene().path), "sections", "txt");
        if(filename.Length != 0)
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filename))
            {
                foreach (var item in sections)
                {
                    writer.WriteLine(item.lower.x);
                    writer.WriteLine(item.lower.y);
                    writer.WriteLine(item.lower.z);
                    writer.WriteLine(item.upper.x);
                    writer.WriteLine(item.upper.y);
                    writer.WriteLine(item.upper.z);
                }
            }
        }
    }

    private string GuessTrackName(DerivedPath path)
    {
        var track = path.GetComponentInParent<Track>();
        if(track)
        {
            return track.Name.ToLower();
        }
        return SceneManager.GetActiveScene().name.ToLower();
    }

    private void ExportObservations(DerivedPath path)
    {
        var filename = EditorUtility.SaveFilePanel("Save Observations", "", (GuessTrackName(path) + ".observations"), "txt");
        if (filename.Length != 0)
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(filename))
            {
                for (float d = 0; d < path.totalLength; d++)
                {
                    var q = path.Query(d);
                    writer.WriteLine(d);
                    writer.WriteLine(q.Curvature);
                    writer.WriteLine(q.Camber);
                    writer.WriteLine(q.Inclination);
                }
            }
        }

    }

    private float[] ImportWeights()
    {
        var weights = new List<float>();
        var filename = EditorUtility.OpenFilePanel("Load Path", System.IO.Path.GetDirectoryName(SceneManager.GetActiveScene().path), "txt");
        if (filename.Length != 0)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(filename))
            {
                while(!reader.EndOfStream)
                {
                    weights.Add(System.Single.Parse(reader.ReadLine()));
                }
            }
        }
        return weights.ToArray();
    }
}
