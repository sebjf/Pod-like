using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

class VehicleTools : EditorWindow
{
    [MenuItem("Tools/Vehicle Tools")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(VehicleTools));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Initialise Rigidbody"))
        {
            InitialiseRigidbody(Selection.activeGameObject);
        }

        if (GUILayout.Button("Initialise Vehicle"))
        {
            InitialiseVehicle(Selection.activeGameObject);
        }

        if (GUILayout.Button("Load Wheel Geometry"))
        {
            LoadWheelGeometry(Selection.activeGameObject);
        }

        if (GUILayout.Button("Initialise Wheels"))
        {
            InitialiseWheels(Selection.activeGameObject);
        }
    }

    void LoadWheelGeometry(GameObject asset)
    {
        // Use the mesh to find the directory of this car.

        var mesh = asset.GetComponentInChildren<MeshFilter>().sharedMesh;
        var name = asset.name;
        var path = Path.GetDirectoryName(AssetDatabase.GetAssetPath(mesh));
        var file = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mesh));

        // get the xml

        var metadatafile = Path.Combine(Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets\\".Length), path), file + ".xml");
        var metadatabytes = File.ReadAllBytes(metadatafile);

        var metadata = AssetsInfo.CarInfo.Load(metadatabytes);

        asset.transform.localPosition = metadata.Chassis.ToUnity();

        // load the wheels

        if(!asset.transform.Find("WheelRearL"))
        {
            var wheelasset = Path.Combine(path, "WheelRearL.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelRearL";
            wheelgameobject.transform.parent = asset.transform;
        }

        if (!asset.transform.Find("WheelFrontL"))
        {
            var wheelasset = Path.Combine(path, "WheelFrontL.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelFrontL";
            wheelgameobject.transform.parent = asset.transform;
        }

        if (!asset.transform.Find("WheelRearR"))
        {
            var wheelasset = Path.Combine(path, "WheelRearR.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelRearR";
            wheelgameobject.transform.parent = asset.transform;
        }

        if (!asset.transform.Find("WheelFrontR"))
        {
            var wheelasset = Path.Combine(path, "WheelFrontR.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelFrontR";
            wheelgameobject.transform.parent = asset.transform;
        }

        var halfWheelHeight = asset.transform.Find("WheelRearL").GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents.y;

        asset.transform.Find("WheelRearL").localPosition = new Vector3(metadata.WheelRearL.x, -metadata.Chassis.y + halfWheelHeight, metadata.WheelRearL.z);
        asset.transform.Find("WheelFrontL").localPosition = new Vector3(metadata.WheelFrontL.x, -metadata.Chassis.y + halfWheelHeight, metadata.WheelFrontL.z);
        asset.transform.Find("WheelRearR").localPosition = new Vector3(metadata.WheelRearR.x, -metadata.Chassis.y + halfWheelHeight, metadata.WheelRearR.z);
        asset.transform.Find("WheelFrontR").localPosition = new Vector3(metadata.WheelFrontR.x, -metadata.Chassis.y + halfWheelHeight, metadata.WheelFrontR.z);
    }

    public static void InitialiseRigidbody(GameObject asset)
    {
        var rb = asset.GetComponent<Rigidbody>();
        if(rb == null)
        {
            rb = asset.AddComponent<Rigidbody>();
        }

        rb.mass = 1500f;
        rb.drag = 0.12f;
        rb.angularDrag = 0.1f;

        var mf = asset.GetComponentInChildren<MeshFilter>();
        var mc = mf.GetComponent<MeshCollider>();
        if(mc == null)
        {
            mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.convex = true;
        }
    }

    public static void InitialiseVehicle(GameObject asset)
    {
        var vehicle = asset.GetComponent<Vehicle>();
        if(vehicle == null)
        {
            vehicle = asset.AddComponent<Vehicle>();
        }

        var drivetrain = asset.GetComponent<Drivetrain>();
        if (drivetrain == null)
        {
            drivetrain = asset.AddComponent<Drivetrain>();
        }

        drivetrain.torqueCurveScalar = 800;
        drivetrain.rpmScalar = 2000;

        drivetrain.torqueCurve = new AnimationCurve();
        var curve = drivetrain.torqueCurve;
        curve.AddKey(new Keyframe(0f, 0.6969147f, 1.056426f, 1.056426f));
        curve.AddKey(new Keyframe(0.6936966f, 0.7885387f, -1.820707f, -1.820707f));
        curve.AddKey(new Keyframe(1f, 0f, 0f, 0f));

        var camrigguid = AssetDatabase.FindAssets("CamRig t:GameObject").First();
        var camrig = Instantiate(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(camrigguid), typeof(GameObject))) as GameObject;
        camrig.transform.SetParent(asset.transform);
        camrig.name = "CamRig";
    }

    public static void InitialiseWheels(GameObject asset)
    {
        WheelTools.AddWheelsForBody(asset.transform);
    }
}