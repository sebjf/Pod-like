using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DeformationModel))]
public class DeformationModelInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("k"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxd"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationsteps"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmo"));
        serializedObject.ApplyModifiedProperties();

        var model = (target as DeformationModel);

        EditorGUILayout.LabelField("Points: " + model.mesh.nodes.Length);
        EditorGUILayout.LabelField("Edges: " + model.mesh.edges.Length);

        if (GUILayout.Button("Create"))
        {
            model.Build();
        }

        if (GUILayout.Button("Step"))
        {
           
        }
    }
}

public class DeformationModelGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
    static void DrawGizmoForDeformationModel(DeformationModel target, GizmoType gizmoType)
    {
        if(!target.gizmo)
        {
            return;
        }

        foreach (var C in target.mesh.nodes)
        {
            var c0 = target.transform.TransformPoint(C.origin);
            var c1 = target.transform.TransformPoint(C.position);
            
            Gizmos.color = new Color((C.origin - C.position).magnitude, 0, 0);
            Gizmos.DrawLine(c0, c1);
            Gizmos.DrawWireSphere(c1, 0.01f);
        }

        foreach (var edge in target.mesh.edges)
        {
            var v0 = target.mesh.nodes[edge.v0];
            var v1 = target.mesh.nodes[edge.v1];

            var c0 = target.transform.TransformPoint(v0.position);
            var c1 = target.transform.TransformPoint(v1.position);

            var violation = edge.length - (v0.position - v1.position).magnitude;

            Gizmos.color = new Color(violation, 0, 0);
            Gizmos.DrawLine(c0, c1);
        }
    }
}
