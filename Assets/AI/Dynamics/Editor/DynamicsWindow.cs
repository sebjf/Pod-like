using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class DynamicsWindow : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Tools/Dynamics")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        DynamicsWindow window = (DynamicsWindow)EditorWindow.GetWindow(typeof(DynamicsWindow));
        window.Show();
    }
    // Update is called once per frame
    void OnGUI()
    {
        if(Selection.activeGameObject == null)
        {
            return;
        }

        var vehicle = Selection.activeGameObject.GetComponent<Vehicle>();

        if(vehicle == null)
        {
            return;
        }

        if(GUILayout.Button("Run"))
        {
            Evaluate(vehicle);           
        }
    }

    public static void Evaluate(Vehicle vehicle)
    {
        var vehicleControllerInput = vehicle.GetComponent<VehicleControllerInput>();
        var autopilot = vehicle.GetComponent<Autopilot>();

        vehicle.throttle = 1f;
        vehicle.brake = 0f;

        Physics.autoSimulation = false;

        vehicle.Awake();

        for (int i = 0; i < 1000; i++)
        {
            vehicle.FixedUpdate();
            Physics.defaultPhysicsScene.Simulate(Time.fixedDeltaTime);
            vehicle.Update();
        }

        Physics.autoSimulation = true;
    }
}
