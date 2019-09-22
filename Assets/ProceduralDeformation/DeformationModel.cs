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
        public float y;         // strain multiplier
        public int locked;      // helper flag to prevent resetting strain of entry node
        public float d;

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
    public float k;     //inelastic stiffness of the material
    public float maxd;  //maximum deformation in world units
    public float geodesicmetric; //applied to the contact node as paramter D - controls the distance of the surface damage element propagation

    public int simulationsteps;

    public FiniteDeformationMesh mesh;

    public int[] nodesmap;

    public bool gizmo;
    public float lastImpactForce;
    public int lastImpactFrame;

    [Serializable]
    public struct Constraint
    {
        public Vector3 position;
        public float weight;
        public float d;
    }

    public Simulation simulation;

    public abstract class Simulation
    {
        public abstract void Step(int iterations);
        public abstract void Release();
    }

    public class GPUSimulation : Simulation
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

            buffers.SetBuffers(deformationshader.Shader, 0, 1, 2, 3);
        }

        private void Step()
        {
            deformationshader.Dispatch(0, mesh.edges.Length, 1, 1);
            deformationshader.Dispatch(1, mesh.nodes.Length, 1, 1);
            deformationshader.Dispatch(2, mesh.nodes.Length, 1, 1);
        }

        public override void Step(int iterations)
        {
            nodes.Buffer.SetData(mesh.nodes);

            for (int i = 0; i < iterations; i++)
            {
                Step();
            }

            nodes.Buffer.GetData(mesh.nodes);
        }

        public override void Release()
        {
            buffers.Release();
        }
    }

    public class CPUSimulation : Simulation
    {
        public CPUSimulation(FiniteDeformationMesh mesh)
        {
            this.mesh = mesh;
            constraints = new Constraint[mesh.totalconstraints];
            previousnodepositions = new Vector3[mesh.nodes.Length];
            nodevelocities = new List<float[]>();
        }

        public Constraint[] constraints;
        public FiniteDeformationMesh mesh;

        public bool logVelocities = true;

        public Vector3[] previousnodepositions;
        public List<float[]> nodevelocities;

        public override void Step(int iterations)
        {
            float[] stepvelocities = new float[iterations];

            for (int i = 0; i < iterations; i++)
            {
                Step();

                if (logVelocities)
                {
                    var maxvelocity = float.MinValue;
                    for (int n = 0; n < mesh.nodes.Length; n++)
                    { 
                        var nodevelocity = (mesh.nodes[n].position - previousnodepositions[n]).magnitude;
                        previousnodepositions[n] = mesh.nodes[n].position;
                        maxvelocity = Mathf.Max(maxvelocity, nodevelocity);
                    }
                    stepvelocities[i] = maxvelocity;
                }
            }

            nodevelocities.Add(stepvelocities);
        }

        private void Step()
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

                constraints[edge.constraintbinv0].d = v1.d - edge.length;
                constraints[edge.constraintbinv1].d = v0.d - edge.length;
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

                if(!(node.locked > 0))
                {
                    for (int i = 0; i < node.constraintcount; i++)
                    {
                        node.d = Mathf.Max(node.d, constraints[node.constraintoffset + i].d);
                    }
                }
            }
        }

        public override void Release()
        {

        }

        public void ExportVelocityLogs(string filename)
        {
            string contents = "";
            foreach (var list in nodevelocities)
            {
                foreach (var item in list)
                {
                    contents += item + ",";
                }
                contents += "\n";
            }
            System.IO.File.WriteAllText(filename, contents);
        }

    }

    // Start is called before the first frame update
    void Start()
    {
        simulation = new GPUSimulation(mesh);
    }

    public void Build()
    {
        var edgemesh = new EdgeMesh();
        var nativemesh = GetComponentInChildren<MeshFilter>().sharedMesh;

        var indices = nativemesh.triangles;
        var positions = nativemesh.vertices;
        var submeshes = Enumerable.Repeat(0, indices.Length).ToArray();

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

        edgemesh.Build(indices, submeshes, edgeMeshVertices.ToArray(), nodesmap);

        mesh = new FiniteDeformationMesh();
        mesh.nodes = vertexsets.Select(v => new Node() { origin = v.position, position = v.position, locked = 0, y = 0 }).ToArray();

        HashSet<long> existingedges = new HashSet<long>();
        List<Edge> newedges = new List<Edge>();
        foreach (var e in edgemesh.edges)
        {
            if(!existingedges.Contains(e.ForwardHash) && !existingedges.Contains(e.OppositeHash))
            {
                newedges.Add(new Edge()
                {
                    v0 = e.node,
                    v1 = e.next.node,
                });
                existingedges.Add(e.ForwardHash);
            }
        }

        mesh.edges = newedges.ToArray();

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

        var force = (collision.impulse / Time.fixedDeltaTime);
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
                closest.position = closest.origin + displacement * transform.InverseTransformDirection(contact.normal);
                closest.y = 100f; // any big number relative to the world scale of the model, since edge strains are the same as world scale deviations
                closest.d = geodesicmetric;
                closest.locked = 1;
            }
        }

        Profiler.EndSample();
        Profiler.BeginSample("Simulate");

        simulation.Step(simulationsteps);

        Profiler.EndSample();

        lastImpactFrame = Time.frameCount;
    }

    public void Step()
    {
        simulation.Step(1);
    }

    private void OnDestroy()
    {
        if (simulation != null)
        {
            simulation.Release();
        }
    }
}
