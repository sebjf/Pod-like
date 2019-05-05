using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EdgeMeshComponentGizmoDrawer
{
    [CustomEditor(typeof(EdgeMeshComponent))]
    public class EdgeMeshComponentInspector : Editor
    {
        float maxEdgeLength;

        public override void OnInspectorGUI()
        {
            var deformer = target as EdgeMeshComponent;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("annotateEdges"));
            serializedObject.ApplyModifiedProperties();

            maxEdgeLength = EditorGUILayout.FloatField("Max Edge Length", maxEdgeLength);

            if (GUILayout.Button("Refine"))
            {
                deformer.Mesh.RefineMesh(maxEdgeLength);
            }

            if (GUILayout.Button("Bake"))
            {
                if (deformer.Mirror == null)
                {
                    deformer.Mirror = new Mesh();
                }
                deformer.Mesh.BakeMesh(deformer.Mirror);
            }

            if (GUILayout.Button("Save Baked Mesh"))
            {
                AssetDatabase.CreateAsset(deformer.Mirror, "Assets/Cars/Alien/Highres.asset");
                AssetDatabase.SaveAssets();
            }
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void DrawGizmoForEdgeMeshComponent(EdgeMeshComponent target, GizmoType gizmoType)
    {
        if(!target.isActiveAndEnabled)
        {
            return;
        }

        var mesh = target.Mesh;

        if(mesh == null)
        {
            return;
        }

        // draw conforming edges

        foreach (var edge in target.Mesh.edges)
        {
            var v0 = edge.vertex;
            var v1 = edge.next.vertex;

            if (edge.Conforming)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(v0.position, v1.position);
            }

            if(edge.Open)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(v0.position, v1.position);
            }
        }

        foreach (var edge in target.Mesh.edges)
        {
            var v0 = edge.vertex;
            var v1 = edge.next.vertex;

            if (!edge.Conforming)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(v0.position, v1.position);
            }
        }

        if (target.annotateEdges)
        {
            foreach (var edge in target.Mesh.edges)
            {
                var v0 = edge.vertex;
                var v1 = edge.next.vertex;

                string edgegui = "";
                edgegui += mesh.edges.IndexOf(edge).ToString() + "\n";
                edgegui += "o1: " + mesh.edges.IndexOf(edge.opposite1) + "\n";
                edgegui += "o2: " + mesh.edges.IndexOf(edge.opposite2) + "\n";
                edgegui += "next : " + mesh.edges.IndexOf(edge.next) + "\n";
                edgegui += "node : " + edge.node;

                Handles.Label(Vector3.Lerp(v0.position, v1.position, 0.4f), edgegui);
            }
        }

    }
}