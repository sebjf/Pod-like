using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DeformerLattice))]
public class DeformerLatticeInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("k"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxd"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("simulationsteps"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmo"));
        serializedObject.ApplyModifiedProperties();

        var model = (target as DeformerLattice);

        EditorGUILayout.LabelField("Points: " + model.mesh.nodes.Count);
        EditorGUILayout.LabelField("Edges: " + model.mesh.edges.Count);

        if (GUILayout.Button("Create"))
        {
            model.Build();
        }

        if (GUILayout.Button("Step"))
        {
            model.Step();
        }
    }
}

public class DeformerLatticeGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
    static void DrawGizmoForMyScript(DeformerLattice target, GizmoType gizmoType)
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
