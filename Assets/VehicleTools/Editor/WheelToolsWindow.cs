using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

class WheelTools : EditorWindow
{
    [MenuItem("Tools/Wheel Tools")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(WheelTools));
    }

    List<Wheel> selectedWheels = new List<Wheel>();

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Selecting the vehicle prefab will automatically select all the wheels.", MessageType.Info); // https://answers.unity.com/questions/1019430

        selectedWheels.Clear();
        foreach (var item in Selection.gameObjects)
        {
            foreach (var wheel in item.GetComponentsInChildren<Wheel>())
            {
                if (!selectedWheels.Contains(wheel))
                {
                    selectedWheels.Add(wheel);
                }
            }
        }

        EditorGUILayout.HelpBox("Use Add Wheel to create the Wheel Component on the selected item. Wheels are added automatically by the Vehicle Tools.", MessageType.Info); // https://answers.unity.com/questions/1019430

        if (GUILayout.Button("Add Wheel"))
        {
            AddWheel(Selection.activeTransform);
        }

        EditorGUILayout.HelpBox("Initialise the wheel with fixed, default settings, and a radius based on the mesh.", MessageType.Info); // https://answers.unity.com/questions/1019430

        if (GUILayout.Button("Reset Default Settings"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetDefaultParameters(wheel);
            }
        }

        if (GUILayout.Button("Reset Radius"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetRadius(wheel);
            }
        }

        EditorGUILayout.LabelField("Suspension Tools");

        EditorGUILayout.HelpBox("Sets the attachment point to be to the height of the center of mass.", MessageType.Info); // https://answers.unity.com/questions/1019430


        if (GUILayout.Button("Set Attachment Point"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetAttachmentPoint(wheel);
            }
        }

        EditorGUILayout.HelpBox("Sets the travel of the wheel based on the current Mesh position.", MessageType.Info); // https://answers.unity.com/questions/1019430


        if (GUILayout.Button("Set Travel"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetTravel(wheel);
            }
        }

        if (GUILayout.Button("Set Stiffness From Mass"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetStiffnessFromMass(wheel);
            }
        }

        EditorGUILayout.LabelField("Pose Controls");

        EditorGUILayout.HelpBox("Sets the Height of the Wheel to what it would be at runtime.", MessageType.Info); // https://answers.unity.com/questions/1019430

        if (GUILayout.Button("Set Height From Mass"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetHeightFromMass(wheel);
            }
        }

        if (GUILayout.Button("Reset Height"))
        {
            foreach (var wheel in selectedWheels)
            {
                ResetHeight(wheel);
            }
        }

        EditorGUILayout.LabelField("Utilities");

        if (GUILayout.Button("Generate Curve Code"))
        {
            GenerateCurveCode(Selection.activeGameObject);
        }
    }

    /// <summary>
    /// Sets K such that at runtime, the resting displacement will be equal to the current pose.
    /// </summary>
    /// <param name="wheel"></param>
    /// <param name="mass"></param>
    public static void SetStiffnessFromMass(Wheel wheel)
    {
        //rigid body
        var rigidBody = wheel.GetComponentInParent<Rigidbody>();
        var mass = rigidBody.mass;

        // target height
        var height = wheel.transform.localPosition.y - wheel.localAttachmentPosition.y;
        var d = wheel.offset + wheel.travel + height;
        var F = (Physics.gravity * mass / 4).magnitude; // assume the mass has already been divided
        var k = F / d;

        wheel.k = k;
        SetHeightFromMass(wheel);

        EditorUtility.SetDirty(wheel);
    }

    /// <summary>
    /// Sets the current wheel height to what it would be if the mass of the car were being simulated at runtime.
    /// (Height is a pose parameter that is usually updated by the simulation)
    /// </summary>
    /// <param name="wheel"></param>
    public static void SetHeightFromMass(Wheel wheel)
    {
        var rigidBody = wheel.GetComponentInParent<Rigidbody>();
        var mass = rigidBody.mass;

        var F = Physics.gravity * mass / 4;
        var d = -F.magnitude / wheel.k;
        var restDistance = wheel.offset + wheel.travel;
        wheel.height = restDistance + d;
    }

    /// <summary>
    /// Sets the current wheel height to the reset pose with no gravity applied.
    /// (Height is a pose parameter that is usually updated by the simulation)
    /// </summary>
    /// <param name="wheel"></param>
    public static void ResetHeight(Wheel wheel)
    {
        wheel.height = wheel.offset + wheel.travel;
    }

    /// <summary>
    /// Sets the Local Attachment Point based on the Parent Rigidbody's Center-of-Mass
    /// </summary>
    public static void SetAttachmentPoint(Wheel wheel)
    {
        var rb = wheel.gameObject.GetComponentInParent<Rigidbody>();
        var com = rb.worldCenterOfMass;
        var plane = new Plane(rb.transform.up, com);

        var worldAttachmentPoint = plane.ClosestPointOnPlane(wheel.transform.position);
        var localAttachmentPoint = rb.transform.worldToLocalMatrix.MultiplyPoint(worldAttachmentPoint);

        wheel.localAttachmentPosition = localAttachmentPoint;

        EditorUtility.SetDirty(wheel);
    }

    /// <summary>
    /// Sets the Travel of the Suspension to be the distance between the Force App Point and the Wheels current Position with a safety margin
    /// </summary>
    /// <param name="wheel"></param>
    public static void SetTravel(Wheel wheel)
    {
        var rb = wheel.gameObject.GetComponentInParent<Rigidbody>();
        var worldAttachmentPoint = rb.transform.localToWorldMatrix.MultiplyPoint(wheel.localAttachmentPosition);
        var length = (worldAttachmentPoint - wheel.transform.position).magnitude;

        Undo.RecordObject(wheel, "Set Wheel Travel");

        wheel.offset = length * 0.25f;
        wheel.travel = length;
    }

    /// <summary>
    /// Set the Radius of the wheel based on any attached geometry
    /// </summary>
    public static void SetRadius(Wheel wheel)
    {
        var meshfilter = wheel.GetComponentInChildren<MeshFilter>();
        if(meshfilter != null)
        {
            var mesh = meshfilter.sharedMesh;
            mesh.RecalculateBounds();

            var max = Mathf.Max(mesh.bounds.extents.x, Mathf.Max(mesh.bounds.extents.y, mesh.bounds.extents.z));
            wheel.radius = max;

            EditorUtility.SetDirty(wheel);
        }
    }

    /// <summary>
    /// Initialises all GameObjects containing the name 'Wheel' with a Wheel Component and sets the default travel
    /// </summary>
    /// <param name="body"></param>
    public static void AddWheelsForBody(Transform body)
    {
        foreach (Transform child in body)
        {
            if(child.name.Contains("Wheel"))
            {
                AddWheel(child);
            }
        }
    }

    public static void AddWheel(Transform transform)
    {
        var gameObject = transform.gameObject;

        var wheel = transform.GetComponent<Wheel>();
        if(wheel == null)
        {
            wheel = gameObject.AddComponent<Wheel>();
        }

        SetDefaultParameters(wheel);
        SetRadius(wheel);
        SetAttachmentPoint(wheel);
        SetTravel(wheel);
        SetStiffnessFromMass(wheel);
    }

    /// <summary>
    /// Sets the Default Parameters Programmatically
    /// </summary>
    /// <param name="wheel"></param>
    public static void SetDefaultParameters(Wheel wheel)
    {
        wheel.k = 60000;
        wheel.B = 10000;
        wheel.mass = 20;
        wheel.brakingTorque = 1000;
        wheel.slipForceScale = 4;
        wheel.forwardSlipScale = 4;

        wheel.slipForce = new AnimationCurve();
        var curve = wheel.slipForce;
        curve.AddKey(new Keyframe(0f, 0f, 0.2f, 0.2f));
        curve.AddKey(new Keyframe(4.996277f, 1f, -0.005135605f, -0.005135605f));

        wheel.forwardSlipForce = new AnimationCurve();
        curve = wheel.forwardSlipForce;
        curve.AddKey(new Keyframe(0f, 0f, 2f, 2f));
        curve.AddKey(new Keyframe(0.1076526f, 0.7259878f, 4.179236f, 4.179236f));
        curve.AddKey(new Keyframe(0.2255114f, 0.9343626f, 0.4801909f, 0.4801909f));
        curve.AddKey(new Keyframe(1f, 1f, 0f, 0f));
    }

    public static void GenerateCurveCode(GameObject gameObject)
    {
        foreach (var component in gameObject.GetComponents(typeof(MonoBehaviour)))
        {
            foreach (var field in component.GetType().GetFields())
            {
                if (field.FieldType == typeof(AnimationCurve))
                {
                    GenerateCurveCode(field.GetValue(component) as AnimationCurve);
                }
            }
        }

    }

    public static void GenerateCurveCode(Wheel wheel)
    {
        GenerateCurveCode(wheel.slipForce);
        GenerateCurveCode(wheel.forwardSlipForce);
    }

    public static void GenerateCurveCode(AnimationCurve curve)
    {
        string function = "";
        function += "//Autogenerated by GenerateCurveCode()\n";
        foreach (var key in curve.keys)
        {
            function += string.Format("curve.AddKey(new Keyframe({0}f, {1}f, {2}f, {3}f));\n", 
                key.time, 
                key.value,
                key.inTangent,
                key.outTangent);
        }
        Debug.Log(function);
    }
}