using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

[Serializable]
public class FiniteDeformationMesh
{
    public FiniteDeformationMesh()
    {
        nodes = new List<Node>();
        edges = new List<Edge>();
    }

    public List<Node> nodes;
    public List<Edge> edges;

    [Serializable]
    public class Node
    {
        public Vector3 origin;
        public Vector3 position;

        public float y = 1f; // strain multiplier

        public bool locked = false;

        public Vector3 displacement
        {
            get
            {
                return position - origin;
            }
        }

        public float strain
        {
            get
            {
                return displacement.magnitude;
            }
        }
    }

    [Serializable]
    public class Edge
    {
        public int v0;
        public int v1;
        public float length;
    }
}

public class DeformerLattice : MonoBehaviour
{
    public float k;
    public float maxd;

    public float simulationsteps;

    public FiniteDeformationMesh mesh;

    public int[] nodesmap;

    public bool gizmo;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Build()
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

        nodesmap = new int[positions.Length];

        for (int i = 0; i < nodesmap.Length; i++)
        {
            nodesmap[i] = -1; // this will make it obvious should the node for a particular vertex fail to be set for some reason
        }

        var vertexsets = map.Sets.ToList();
        foreach (var set in vertexsets)
        {
            foreach (var index in set.indices)
            {
                nodesmap[index] = vertexsets.IndexOf(set);
            }
        }

        var edgeMeshVertices = new List<EdgeMesh.Vertex>();
        edgeMeshVertices.AddRange(positions.Select(v => new EdgeMesh.Vertex() { position = v }));

        edgemesh.Build(indices, edgeMeshVertices.ToArray(), nodesmap);

        mesh = new FiniteDeformationMesh();
        mesh.nodes.AddRange(
            vertexsets.Select(v => new FiniteDeformationMesh.Node() { origin = v.position, position = v.position })
            );

        mesh.edges.AddRange(
            edgemesh.edges.Select(
                e => new FiniteDeformationMesh.Edge()
                {
                    v0 = e.node,
                    v1 = e.next.node,
                }
                ));

        foreach (var edge in mesh.edges)
        {
            edge.length = (mesh.nodes[edge.v0].origin - mesh.nodes[edge.v1].origin).magnitude;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Profiler.BeginSample("Damage Force Application");

        var force = transform.InverseTransformDirection(collision.impulse / Time.fixedDeltaTime);

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            var point = transform.InverseTransformPoint(contact.point);

            Debug.Log(force.magnitude);

            var closest = mesh.nodes.First();
            foreach (var item in mesh.nodes)
            {
                if((item.origin - point).magnitude < (closest.origin - point).magnitude)
                {
                    closest = item;
                }
            }

            var displacement = force.magnitude / k;
            displacement = Mathf.Min(displacement, maxd);

            if (displacement > closest.strain)
            {
                closest.position = closest.origin + displacement * force.normalized;
                closest.y = 100f; // any big number relative to the world scale of the model, since edge strains are the same as world scale deviations
                closest.locked = true;
            }
        }

        Profiler.EndSample();
        Profiler.BeginSample("Simulate");

        for (int i = 0; i < simulationsteps; i++)
        {
            Step();
        }

        Profiler.EndSample();

        foreach (var node in mesh.nodes)
        {
            node.locked = false;
        }
    }

    public void Step()
    {
        // resolve the constraints one by one

        foreach (var edge in mesh.edges)
        {
            var v0 = mesh.nodes[edge.v0];
            var v1 = mesh.nodes[edge.v1];

            var V = (v1.position - v0.position);

            var violation = V.magnitude - edge.length;

            var correction0 = V.normalized *  violation * (v1.y / (v0.y + v1.y + Mathf.Epsilon));
            var correction1 = V.normalized * -violation * (v0.y / (v0.y + v1.y + Mathf.Epsilon));

            v0.position += correction0;
            v1.position += correction1;
        }

        foreach (var node in mesh.nodes)
        {
            if (!node.locked)
            {
                node.y = 0f;
            }
        }

        foreach (var edge in mesh.edges)
        {
            var v0 = mesh.nodes[edge.v0];
            var v1 = mesh.nodes[edge.v1];
            var V = (v1.position - v0.position);
            var violation = Mathf.Abs(V.magnitude - edge.length);
            v0.y += violation;
            v1.y += violation;
        }
    }

}
