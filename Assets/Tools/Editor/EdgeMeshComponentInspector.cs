using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EdgeMeshComponentGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForMyScript(EdgeMeshComponent target, GizmoType gizmoType)
    {
        var mesh = target.Mesh;

        // draw conforming edges

        foreach (var edge in target.Mesh.edges)
        {
            var v0 = mesh.vertices[edge.vertex];
            var v1 = mesh.vertices[edge.next.vertex];

            Gizmos.color = Color.white;

            string edgegui = "";
            edgegui += mesh.edges.IndexOf(edge).ToString() + "\n";
            edgegui += "o1: " + mesh.edges.IndexOf(edge.opposite1) + "\n";
            edgegui += "o2: " + mesh.edges.IndexOf(edge.opposite2) + "\n";
            edgegui += "n : " + mesh.edges.IndexOf(edge.next);

            if (edge.Conforming)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(v0.position, v1.position);
                Handles.Label(Vector3.Lerp(v0.position, v1.position, 0.4f), edgegui);
            }

            if(edge.Open)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(v0.position, v1.position);
                Handles.Label(Vector3.Lerp(v0.position, v1.position, 0.4f), edgegui);
            }
        }

        foreach (var edge in target.Mesh.edges)
        {
            var v0 = mesh.vertices[edge.vertex];
            var v1 = mesh.vertices[edge.next.vertex];

            Gizmos.color = Color.white;

            string edgegui = "";
            edgegui += mesh.edges.IndexOf(edge).ToString() + "\n";
            edgegui += "o1: " + mesh.edges.IndexOf(edge.opposite1) + "\n";
            edgegui += "o2: " + mesh.edges.IndexOf(edge.opposite2) + "\n";
            edgegui += "n : " + mesh.edges.IndexOf(edge.next);

            if (!edge.Conforming)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(v0.position, v1.position);
                Handles.Label(Vector3.Lerp(v0.position, v1.position, 0.4f), edgegui);
            }
        }

    }
}