using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Profiling;

using Node = FiniteDeformationMesh.Node;
using Edge = FiniteDeformationMesh.Edge;

[Serializable]
public class FiniteDeformationMesh
{
    public Node[] nodes;
    public Edge[] edges;

    public int totalconstraints;

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Node
    {
        public Vector3 origin;
        public Vector3 position;
        public float y;        // strain multiplier
        public int locked; // helper flag to prevent resetting strain of entry node

        public int constraintoffset;
        public int constraintcount;

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
    [StructLayout(LayoutKind.Sequential)]
    public struct Edge
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

    public int simulationsteps;

    public FiniteDeformationMesh mesh;

    public int[] nodesmap;

    public bool gizmo;
    public float lastImpactForce;

    [Serializable]
    public struct Constraint
    {
        public Vector3 position;
        public float weight;
    }

    GPUSimulation simulation;

    public class GPUSimulation
    {
        public GPUBuffer nodes;

        FiniteDeformationMesh mesh;
        BuffersHelper buffers;
        ShaderWrapper deformationshader;

        public GPUSimulation(FiniteDeformationMesh mesh)
        {
            this.mesh = mesh;
            buffers = new BuffersHelper();
            nodes = new GPUBuffer<Node>(mesh.nodes);
            buffers.Add(nodes, "nodes");
            buffers.Add(new GPUBuffer<Edge>(mesh.edges), "edges");
            buffers.Add(new GPUBuffer<Constraint>(mesh.totalconstraints), "constraints");
            deformationshader = new ShaderWrapper("DeformationModel");

            deformationshader.Shader.SetInt("numnodes", mesh.nodes.Length);
            deformationshader.Shader.SetInt("numedges", mesh.edges.Length);
            deformationshader.Shader.SetInt("numconstraints", mesh.totalconstraints);

            buffers.SetBuffers(deformationshader.Shader, 0, 1, 2, 3, 4, 5);
        }

        public void Step()
        {
            deformationshader.Dispatch(0, mesh.totalconstraints, 1, 1);
            deformationshader.Dispatch(1, mesh.edges.Length, 1, 1);
            deformationshader.Dispatch(2, mesh.nodes.Length, 1, 1);
            deformationshader.Dispatch(3, mesh.edges.Length, 1, 1);
            deformationshader.Dispatch(4, mesh.nodes.Length, 1, 1);
        }

        public void Step(int iterations)
        {
            nodes.Buffer.SetData(mesh.nodes);

            for (int i = 0; i < iterations; i++)
            {
                Step();
            }

            deformationshader.Dispatch(5, mesh.nodes.Length, 1, 1);

            nodes.Buffer.GetData(mesh.nodes);
        }

        public void Release()
        {
            buffers.Release();
        }
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

        public void Step(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                Step();
            }

            for (int i = 0; i < mesh.nodes.Length; i++)
            {
                ref var node = ref mesh.nodes[i];
                node.locked = 0;
            }
        }

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

            for(int i = 0; i < mesh.nodes.Length; i++)
            {
                if (mesh.nodes[i].locked <= 0)
                {
                    mesh.nodes[i].y = 0f;
                }
            }

            foreach (var edge in mesh.edges)
            {
                ref var v0 = ref mesh.nodes[edge.v0];
                ref var v1 = ref mesh.nodes[edge.v1];
                var V = (v1.position - v0.position);
                var violation = Mathf.Abs(V.magnitude - edge.length);
                v0.y += violation;
                v1.y += violation;
            }

            for(int n = 0; n < mesh.nodes.Length; n++)
            {
                ref var node = ref mesh.nodes[n];

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

        public void Release()
        {

        }

    }

    // Start is called before the first frame update
    void Start()
    {
        simulation = new GPUSimulation(mesh);
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
        mesh.nodes = vertexsets.Select(v => new Node() { origin = v.position, position = v.position, locked = 0, y = 0 }).ToArray();

        mesh.edges =
            edgemesh.edges.Select(
                e => new Edge()
                {
                    v0 = e.node,
                    v1 = e.next.node,
                }
                ).ToArray();

        for (int i = 0; i < mesh.edges.Length; i++)
        {
            ref var edge = ref mesh.edges[i];
            edge.length = (mesh.nodes[edge.v0].origin - mesh.nodes[edge.v1].origin).magnitude;
        }

        for (int i = 0; i < mesh.edges.Length; i++)
        {
            ref var edge = ref mesh.edges[i];

            edge.constraintbinv0 = mesh.nodes[edge.v0].constraintcount;
            mesh.nodes[edge.v0].constraintcount++;

            edge.constraintbinv1 = mesh.nodes[edge.v1].constraintcount;
            mesh.nodes[edge.v1].constraintcount++;
        }

        mesh.totalconstraints = 0;

        for (int i = 0; i < mesh.nodes.Length; i++)
        {
            mesh.nodes[i].constraintoffset = mesh.totalconstraints;
            mesh.totalconstraints += mesh.nodes[i].constraintcount;
        }

        for (int i = 0; i < mesh.edges.Length; i++)
        {
            ref var edge = ref mesh.edges[i];

            edge.constraintbinv0 += mesh.nodes[edge.v0].constraintoffset;
            edge.constraintbinv1 += mesh.nodes[edge.v1].constraintoffset;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Profiler.BeginSample("Damage Force Application");

        var force = transform.InverseTransformDirection(collision.impulse / Time.fixedDeltaTime);
        lastImpactForce = force.magnitude;

        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.GetContact(i);
            var point = transform.InverseTransformPoint(contact.point);

            ref var closest = ref mesh.nodes[0];
            for (int n = 0; n < mesh.nodes.Length; n++)
            {
                ref var item = ref mesh.nodes[n];
                if((item.origin - point).magnitude < (closest.origin - point).magnitude)
                {
                    closest = ref item;
                }
            }

            var displacement = force.magnitude / k;
            displacement = Mathf.Min(displacement, maxd);

            if (displacement > closest.strain)
            {
                closest.position = closest.origin + displacement * force.normalized;
                closest.y = 100f; // any big number relative to the world scale of the model, since edge strains are the same as world scale deviations
                closest.locked = 1;
            }
        }

        Profiler.EndSample();
        Profiler.BeginSample("Simulate");

        simulation.Step(simulationsteps);

        Profiler.EndSample();
    }

    public void Step()
    {
        simulation.Step(1);
    }

    private void OnDestroy()
    {
        simulation.Release();
    }
}
