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

    public int totalconstraints;

    [Serializable]
    public class Node
    {
        public Vector3 origin;
        public Vector3 position;
        public float y = 1f;        // strain multiplier
        public bool locked = false; // helper flag to prevent resetting strain of entry node

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

        public int constraintoffset;
        public int constraintcount;
    }

    [Serializable]
    public class Edge
    {
        public int v0;
        public int v1;
        public float length;

        public int constraintbinv0;
        public int constraintbinv1;
    }
}

public class DeformationModel : MonoBehaviour
{
    public float k;
    public float maxd;

    public float simulationsteps;

    public FiniteDeformationMesh mesh;

    public int[] nodesmap;

    public bool gizmo;

    [Serializable]
    public struct Constraint
    {
        public Vector3 position;
        public float weight;
    }

    CPUSimulation simulation;

    public class GPUSimulation
    {


    }

    public class CPUSimulation
    {
        public CPUSimulation(FiniteDeformationMesh mesh)
        {
            this.mesh = mesh;
            constraints = new Constraint[mesh.totalconstraints];
        }

        public Constraint[] constraints;
        public FiniteDeformationMesh mesh;

        public void Step()
        {
            // resolve the constraints one by one

            for (int i = 0; i < constraints.Length; i++)
            {
                constraints[i].weight = 0;
            }

            foreach (var edge in mesh.edges)
            {
                var v0 = mesh.nodes[edge.v0];
                var v1 = mesh.nodes[edge.v1];

                var V = (v1.position - v0.position);

                var violation = V.magnitude - edge.length;

                var correction0 = V.normalized * violation * (v1.y / (v0.y + v1.y + Mathf.Epsilon));
                var correction1 = V.normalized * -violation * (v0.y / (v0.y + v1.y + Mathf.Epsilon));

                constraints[edge.constraintbinv0].position = v0.position + correction0;
                constraints[edge.constraintbinv0].weight = Mathf.Abs(violation);

                constraints[edge.constraintbinv1].position = v1.position + correction1;
                constraints[edge.constraintbinv1].weight = Mathf.Abs(violation);
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

            foreach (var node in mesh.nodes)
            {
                Vector3 positions = Vector3.zero;
                float weight = 0;

                for (int i = 0; i < node.constraintcount; i++)
                {
                    positions += constraints[node.constraintoffset + i].position * constraints[node.constraintoffset + i].weight;
                    weight += constraints[node.constraintoffset + i].weight;
                }

                if (weight > Mathf.Epsilon)
                {
                    node.position = positions / weight;
                }
            }
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        simulation = new CPUSimulation(mesh);
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

        foreach (var edge in mesh.edges)
        {
            edge.constraintbinv0 = mesh.nodes[edge.v0].constraintcount;
            mesh.nodes[edge.v0].constraintcount++;

            edge.constraintbinv1 = mesh.nodes[edge.v1].constraintcount;
            mesh.nodes[edge.v1].constraintcount++;
        }

        mesh.totalconstraints = 0;

        foreach (var node in mesh.nodes)
        {
            node.constraintoffset = mesh.totalconstraints;
            mesh.totalconstraints += node.constraintcount;
        }

        foreach (var edge in mesh.edges)
        {
            edge.constraintbinv0 += mesh.nodes[edge.v0].constraintoffset;
            edge.constraintbinv1 += mesh.nodes[edge.v1].constraintoffset;
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
        simulation.Step();
    }
}
