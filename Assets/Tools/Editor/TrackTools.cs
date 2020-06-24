using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

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
}

