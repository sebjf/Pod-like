using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EdgeMeshComponent : MonoBehaviour
{
    [HideInInspector]
    public EdgeMesh Mesh;

    public void Reset()
    {
        var edgemesh = new EdgeMesh();
        var nativemesh = GetComponentInChildren<MeshFilter>().sharedMesh;

        var indices = nativemesh.triangles;
        var positions = nativemesh.vertices;

        // collect the vertices into nodes/groups based on their location

        SpatialHashMap map = new SpatialHashMap(0.01f);
        for (int i = 0; i < positions.Length; i++)
        {
            map.Add(positions[i], i);
        }

        var nodes = new int[positions.Length];

        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = -1; // this will make it obvious should the node for a particular vertex fail to be set for some reason
        }

        var vertexsets = map.Sets.ToList();
        foreach (var set in vertexsets)
        {
            foreach (var index in set.indices)
            {
                nodes[index] = vertexsets.IndexOf(set);
            }
        }

        var edgeMeshVertices = new List<EdgeMesh.Vertex>();
        edgeMeshVertices.AddRange(positions.Select(v => new EdgeMesh.Vertex() { position = v }));

        edgemesh.Build(indices, edgeMeshVertices.ToArray(), nodes);

        var fem = GetComponent<DeformationModel>();

        fem.mesh = new FiniteDeformationMesh();
        fem.mesh.nodes = (
            vertexsets.Select(v => new FiniteDeformationMesh.Node() { origin = v.position })
            ).ToArray();

        fem.mesh.edges = (
            edgemesh.edges.Select(
                e => new FiniteDeformationMesh.Edge()
                {
                    v0 = e.node,
                    v1 = e.next.node,
                }
                )).ToArray();

        for (int i = 0; i < fem.mesh.edges.Length; i++)
        {
            var edge = fem.mesh.edges[i];
            fem.mesh.edges[i].length = (fem.mesh.nodes[edge.v0].origin - fem.mesh.nodes[edge.v1].origin).magnitude;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
