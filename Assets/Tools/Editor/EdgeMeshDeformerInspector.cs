using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EdgeMeshDeformer))]
public class EdgeMeshDeformerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var deformer = target as EdgeMeshDeformer;

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("edgeid"));

        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Refine"))
        {
            deformer.Refine(deformer.edgeid);
        }

        if (GUILayout.Button("Fix"))
        {
            deformer.Correct();
        }
    }
}

public class EdgeMeshDeformerGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForMyScript(EdgeMeshDeformer target, GizmoType gizmoType)
    {
        var mesh = target.mesh;

        var v0 = mesh.vertices[mesh.edges[target.edgeid].vertex];
        var v1 = mesh.vertices[mesh.edges[target.edgeid].next.vertex];

        Gizmos.color = Color.green;

        if (target.isActiveAndEnabled)
        {
            Gizmos.DrawLine(v0.position, v1.position);
        }
    }
}