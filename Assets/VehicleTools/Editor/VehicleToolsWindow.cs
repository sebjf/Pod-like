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
        EditorGUILayout.HelpBox("To begin, create a new prefab and manually place the wheels. Select the prefab in the scene and press the buttons below.", MessageType.Info); // https://answers.unity.com/questions/1019430

        if (GUILayout.Button("Initialise Vehicle Body"))
        {
            InitialiseVehicle(Selection.activeGameObject);
        }

        if (GUILayout.Button("Initialise Wheels"))
        {
            InitialiseWheels(Selection.activeGameObject);
        }

        EditorGUILayout.HelpBox("After initialising the body and wheels, use the Wheel Tools window to configure the wheels", MessageType.Info); // https://answers.unity.com/questions/1019430

        EditorGUILayout.LabelField("Physics");

        EditorGUILayout.HelpBox("After setting center of mass, press W to return to the Gizmo", MessageType.Info); // https://answers.unity.com/questions/1019430


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

    public static void InitialiseVehicle(GameObject asset)
    {
        var rb = asset.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = asset.AddComponent<Rigidbody>();

            rb.mass = 1500f;
            rb.drag = 0.12f;
            rb.angularDrag = 0.1f;
        }

        var mf = asset.GetComponentInChildren<MeshFilter>();
        var mc = mf.GetComponent<MeshCollider>();
        if (mc == null)
        {
            mc = mf.gameObject.AddComponent<MeshCollider>();
            mc.convex = true;
        }

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

            drivetrain.torqueCurve = new AnimationCurve();
            var curve = drivetrain.torqueCurve;
            curve.AddKey(new Keyframe(0f, 800f, -0.04290399f, -0.04290399f));
            curve.AddKey(new Keyframe(1825.365f, 167.7371f, -2.252331f, -2.252331f));
            curve.AddKey(new Keyframe(2000f, 0f, -0.01201525f, -0.01201525f));
        }

        var camrigguid = AssetDatabase.FindAssets("CamRig t:GameObject").First();
        var camrig = Instantiate(AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(camrigguid), typeof(GameObject))) as GameObject;
        camrig.transform.SetParent(asset.transform);
        camrig.name = "CamRig";
    }

    public static void InitialiseWheels(GameObject asset)
    {
        WheelTools.AddWheelsForBody(asset.transform);
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