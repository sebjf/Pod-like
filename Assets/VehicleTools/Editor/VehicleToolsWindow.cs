using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;

class VehicleTools : EditorWindow
{
    [MenuItem("Tools/Vehicle Tools")]

    public static void ShowWindow()
    {
        var window = EditorWindow.GetWindow(typeof(VehicleTools)) as VehicleTools;
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    bool setCenterOfMass = false;
    Tool tool;

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

        EditorGUILayout.LabelField("Physics");

        setCenterOfMass = GUILayout.Toggle(setCenterOfMass, "Set Center Of Mass", "Button");

        EditorGUILayout.LabelField("Deformation");

        if (GUILayout.Button("Create High Res Geometry"))
        {
            CreateHighResolutionGeometry(Selection.activeGameObject);
        }

        if (GUILayout.Button("Create Deformation Model"))
        {
            CreateDeformationComponents(Selection.activeGameObject);
        }
    }

    public void OnSceneGUI(SceneView view)
    {
        if (setCenterOfMass)
        {
            Tools.current = Tool.None;
            try
            {
                var rb = Selection.activeGameObject.GetComponent<Rigidbody>();

                EditorGUI.BeginChangeCheck();
                var worldCoM = (Handles.PositionHandle(rb.transform.TransformPoint(rb.centerOfMass), Quaternion.identity)); if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(rb, "Change Center of Mass");
                    rb.centerOfMass = rb.transform.InverseTransformPoint(worldCoM);
                }

                HandleUtility.Repaint();
            }
            catch (NullReferenceException)
            {
            }
        }
    }

    void LoadWheelGeometry(GameObject asset)
    {
        // Use the mesh to find the directory of this car.

        var paths = AssetTools.FindAssetPaths(asset);

        // load the wheels

        if(!asset.transform.Find("WheelRearL"))
        {
            var wheelasset = Path.Combine(paths.directory, "WheelRearL.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelRearL";
            wheelgameobject.transform.parent = asset.transform;
        }

        if (!asset.transform.Find("WheelFrontL"))
        {
            var wheelasset = Path.Combine(paths.directory, "WheelFrontL.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelFrontL";
            wheelgameobject.transform.parent = asset.transform;
        }

        if (!asset.transform.Find("WheelRearR"))
        {
            var wheelasset = Path.Combine(paths.directory, "WheelRearR.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelRearR";
            wheelgameobject.transform.parent = asset.transform;
        }

        if (!asset.transform.Find("WheelFrontR"))
        {
            var wheelasset = Path.Combine(paths.directory, "WheelFrontR.obj");
            var wheelgameobject = Instantiate(AssetDatabase.LoadAssetAtPath(wheelasset, typeof(GameObject))) as GameObject;
            wheelgameobject.name = "WheelFrontR";
            wheelgameobject.transform.parent = asset.transform;
        }
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
        var controllerinput = asset.GetComponent<VehicleControllerInput>();
        if (controllerinput == null)
        {
            controllerinput = asset.AddComponent<VehicleControllerInput>();
        }

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

        var wheelManager = asset.GetComponent<WheelManager>();
        if(wheelManager == null)
        {
            wheelManager = asset.AddComponent<WheelManager>();
            wheelManager.Reset();
        }
    }

    public static void CreateHighResolutionGeometry(GameObject asset)
    {
        var paths = AssetTools.FindAssetPaths(asset);
        var original = AssetDatabase.LoadAssetAtPath(paths.file + ".obj", typeof(Mesh)) as Mesh;

        var deformable = AssetDatabase.LoadAssetAtPath(paths.file + "DeformableMesh.asset", typeof(Mesh)) as Mesh;
        if(deformable == null)
        {
            deformable = new Mesh();
            AssetDatabase.CreateAsset(deformable, paths.file + "DeformableMesh.asset");
        }

        EdgeMesh edgemesh = new EdgeMesh();
        edgemesh.Build(original);
        edgemesh.RefineMesh(0.25f);
        edgemesh.BakeMesh(deformable);

        AssetDatabase.SaveAssets();

        asset.GetComponentInChildren<MeshFilter>().sharedMesh = deformable;
    }

    public static void CreateDeformationComponents(GameObject asset)
    {
        var model = asset.GetComponent<DeformationModel>();
        if (model == null)
        {
            model = asset.AddComponent<DeformationModel>();
        }

        model.Build();
        model.k = 400000;
        model.maxd = 0.6f;
        model.geodesicmetric = 0.8f;
        model.simulationsteps = 25;

        EditorUtility.SetDirty(model);

        var filter = asset.GetComponentInChildren<MeshFilter>();

        var dynamicrenderer = filter.gameObject.GetComponent<DynamicMesh>();

        if(dynamicrenderer == null)
        {
            dynamicrenderer = filter.gameObject.AddComponent<DynamicMesh>();
        }
    }
}