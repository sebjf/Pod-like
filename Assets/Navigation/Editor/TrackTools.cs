using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.Xml.Schema;

public class TrackTools : EditorWindow
{
    [MenuItem("Tools/Track Tools")]
    static void Init()
    {
        var window = (TrackTools)EditorWindow.GetWindow(typeof(TrackTools));
        window.Show();
    }

    private Material replace;
    private Material with;

    void OnGUI()
    {
        if (GUILayout.Button("Add Race Camera"))
        {
            AddRaceCamera();
        }
    }

    public static void GenerateColliders(GameObject gameobject)
    { 
        var meshimporter = AssetImporter.GetAtPath(AssetTools.FindAssetPaths(gameobject).assetpath) as ModelImporter;
    }

    public static void AddRaceCamera()
    {
        var rc = GameObject.Find("Race Camera");
        if (rc == null)
        { 
            GameObject rcasset = (GameObject)AssetDatabase.LoadAssetAtPath(@"Assets/Prefabs/Cameras/Race Camera.prefab", typeof(GameObject));
            PrefabUtility.InstantiatePrefab(rcasset);
        }
    }

    public static void SetStartWaypoint(TrackGeometry geometry, TrackWaypoint waypoint)
    {
        var index = geometry.waypoints.IndexOf(waypoint);

        // get distance through which we are moving the start

        var offset = geometry.totalLength - geometry.waypoints[index].Distance;

        // first cycle the waypoints. distances are defined by position, so we don't need to change the distances, just the order, 
        // then recompute

        Undo.RecordObject(geometry, "Set Starting Line");

        {
            var toMove = geometry.waypoints.GetRange(0, index);
            geometry.waypoints.RemoveRange(0, index);
            geometry.waypoints.AddRange(toMove);
            geometry.Recompute();
        }

        foreach (var item in geometry.gameObject.GetComponentsInChildren<DerivedPath>())
        {
            Undo.RecordObject(item, "Set Starting Line");
            foreach (var wp in item.waypoints)
            {
                wp.x = Mathf.Repeat(wp.x + offset, geometry.totalLength);
            }

            int smallest = 0;
            for (int i = 0; i < item.waypoints.Count; i++)
            {
                if(item.waypoints[i].x < item.waypoints[smallest].x)
                {
                    smallest = i;
                }
            }
            var toMove = item.waypoints.GetRange(0, smallest);
            item.waypoints.RemoveRange(0, smallest);
            item.waypoints.AddRange(toMove);
            item.Recompute();
        }
    }
}

