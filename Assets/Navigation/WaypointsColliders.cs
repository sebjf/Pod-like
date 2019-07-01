using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Waypoints))]
public class WaypointsColliders : MonoBehaviour
{
    public float height = 0.5f;

    public void Rebuild()
    {
        var waypoints = GetComponent<Waypoints>();

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        foreach (var waypoint in waypoints.waypoints)
        {
            var start = waypoints.Edges(waypoint.start);
            var end = waypoints.Edges(waypoint.end);

            int starttriangle = vertices.Count;

            vertices.Add(start.left - Vector3.up * height);
            vertices.Add(end.left - Vector3.up * height);
            vertices.Add(end.left + Vector3.up * height);
            vertices.Add(start.left + Vector3.up * height);

            triangles.Add(starttriangle + 0);
            triangles.Add(starttriangle + 1);
            triangles.Add(starttriangle + 2);
            
            triangles.Add(starttriangle + 2);
            triangles.Add(starttriangle + 3);
            triangles.Add(starttriangle + 0);

            starttriangle = vertices.Count;

            vertices.Add(start.right - Vector3.up * height);
            vertices.Add(end.right - Vector3.up * height);
            vertices.Add(end.right + Vector3.up * height);
            vertices.Add(start.right + Vector3.up * height);

            triangles.Add(starttriangle + 2);
            triangles.Add(starttriangle + 1);
            triangles.Add(starttriangle + 0);

            triangles.Add(starttriangle + 0);
            triangles.Add(starttriangle + 3);
            triangles.Add(starttriangle + 2);
        }

        var mesh = new Mesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        var collider = GetComponent<MeshCollider>();
        if(collider == null)
        {
            collider = gameObject.AddComponent<MeshCollider>();
        }

        collider.sharedMesh = mesh;
    }
}
