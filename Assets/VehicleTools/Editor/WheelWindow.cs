using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

class WheelWindow : EditorWindow
{
    [MenuItem("Window/Wheel Editor")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(WheelWindow));
    }

    List<Wheel> selectedWheels = new List<Wheel>();

    void OnGUI()
    {
        selectedWheels.Clear();
        foreach (var item in Selection.gameObjects)
        {
            foreach (var wheel in item.GetComponentsInChildren<Wheel>())
            {
                if(!selectedWheels.Contains(wheel))
                {
                    selectedWheels.Add(wheel);
                }
            }
        }

        if(GUILayout.Button("Set Stiffness"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetSpringStiffness(wheel);
            }
        }

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

        if(GUILayout.Button("Set Radius"))
        {
            foreach (var wheel in selectedWheels)
            {
                SetRadius(wheel);
            }
        }
    }

    /// <summary>
    /// Sets K such that at runtime, the resting displacement will be equal to the current pose.
    /// </summary>
    /// <param name="wheel"></param>
    /// <param name="mass"></param>
    public static void SetSpringStiffness(Wheel wheel)
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
    }

    public static void SetHeightFromMass(Wheel wheel)
    {
        var rigidBody = wheel.GetComponentInParent<Rigidbody>();
        var mass = rigidBody.mass;

        var F = Physics.gravity * mass / 4;
        var d = -F.magnitude / wheel.k;
        var restDistance = wheel.offset + wheel.travel;
        wheel.height = restDistance + d;
    }

    public static void ResetHeight(Wheel wheel)
    {
        wheel.height = wheel.offset + wheel.travel;
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
        }
    }
}