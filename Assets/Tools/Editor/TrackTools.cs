using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class CircuitInfoExtensions
{
    public static Vector3 ToUnity(this AssetsInfo.Vector3 v)
    {
        Vector3 result = new Vector3();
        result.x = -v.x;
        result.y = v.y;
        result.z = v.z;
        return result;
    }
}


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
        if(GUILayout.Button("Initialise"))
        {
            Initialise(Selection.activeGameObject);
        }

        if (GUILayout.Button("Add Race Camera"))
        {
            AddRaceCamera();
        }
    }

    public static void Initialise(GameObject gameobject)
    {
        var metadata = AssetTools.FindAssetMetadataBytes(gameobject);
        var circuitinfo = AssetsInfo.CircuitInfo.Load(metadata);

        if(gameobject.GetComponent<Circuit>() == null)
        {
            gameobject.AddComponent<Circuit>();
            ImportCircuitInfo(gameobject.GetComponent<Circuit>(), circuitinfo);
        }

        //GenerateColliders(gameobject);
        AddRaceCamera();
    }

    public static void GenerateColliders(GameObject gameobject)
    { 
        var meshimporter = AssetImporter.GetAtPath(AssetTools.FindAssetPaths(gameobject).assetpath) as ModelImporter;
    }

    public static void ImportCircuitInfo(Circuit component, AssetsInfo.CircuitInfo circuit)
    {
        // process grid positions

        component.GridPositions = new Vector3[8];

        var pole = circuit.grid.pole.ToUnity();
        var second = circuit.grid.second.ToUnity();
        var third = circuit.grid.third.ToUnity();
        var forward = circuit.grid.forward.ToUnity();

        var deltax = second - pole;
        var deltay = third - pole;

        var gridPositions = new List<Vector3>();

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                var position = pole + (deltay * y) + (deltax * x);
                gridPositions.Add(position);
            }
        }

        component.StartDirection = forward;

        component.GridPositions = gridPositions.ToArray();
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

