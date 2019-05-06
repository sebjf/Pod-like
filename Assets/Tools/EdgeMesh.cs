using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EdgeMesh
{
    public EdgeMesh()
    {
        vertices = new List<Vertex>();
        edges = new List<Edge>();
    }

    public class Vertex
    {
        public int node;
        public int index;

        public Vector3 position;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 uv ;
        public Vector2 uv2;
        public Vector2 uv3;
        public Vector2 uv4;
        public Vector2 uv5;
        public Vector2 uv6;
        public Vector2 uv7;
        public Vector2 uv8;

        public static Vertex Interpolate(Vertex v0, Vertex v1)
        {
            var v = new Vertex();
            v.position = Vector3.Lerp(v0.position, v1.position, 0.5f);
            v.normal = Vector3.Lerp(v0.normal, v1.normal, 0.5f);
            v.tangent = Vector4.Lerp(v0.tangent, v1.tangent, 0.5f);
            v.uv  = Vector2.Lerp(v0.uv,  v1.uv, 0.5f);
            v.uv2 = Vector2.Lerp(v0.uv2, v1.uv2, 0.5f);
            v.uv3 = Vector2.Lerp(v0.uv3, v1.uv3, 0.5f);
            v.uv4 = Vector2.Lerp(v0.uv4, v1.uv4, 0.5f);
            v.uv5 = Vector2.Lerp(v0.uv5, v1.uv5, 0.5f);
            v.uv6 = Vector2.Lerp(v0.uv6, v1.uv6, 0.5f);
            v.uv7 = Vector2.Lerp(v0.uv7, v1.uv7, 0.5f);
            v.uv8 = Vector2.Lerp(v0.uv7, v1.uv8, 0.5f);
            return v;
        }
    }

    public class Edge
    {
        public int submesh;
        public int node;
        public Vertex vertex;
        public Edge next;
        public Edge opposite1;
        public Edge opposite2;

        public Edge()
        {
            opposite1 = null;
            opposite2 = null;
        }

        public bool Conforming
        {
            get
            {
                if(opposite1 != opposite2 && opposite2 != null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public bool Open
        {
            get
            {
                return opposite1 == null && opposite2 == null;
            }
        }

        public long ForwardHash
        {
            get
            {
                long hash = node;
                hash = hash << 32;
                hash = hash | (uint)next.node;
                return hash;
            }
        }

        public long OppositeHash
        {
            get
            {
                long hash = next.node;
                hash = hash << 32;
                hash = hash | (uint)node;
                return hash;
            }
        }

        public float Length
        {
            get
            {
                return (vertex.position - next.vertex.position).magnitude;
            }
        }

    }

    public List<Vertex> vertices;
    public List<Edge> edges;
    public int nodes;

    public class BakeOptions
    {
        public bool normals;
        public bool tangents;
        public bool uv;
        public bool uv2;
        public bool uv3;
        public bool uv4;
        public bool uv5;
        public bool uv6;
        public bool uv7;
        public bool uv8;
    }

    public BakeOptions bakeoptions;

    public void Build(Mesh nativemesh)
    {
        var vertices = new List<Vertex>();

        var positions = nativemesh.vertices;

        var normals = nativemesh.normals;
        var tangents = nativemesh.tangents;
        var uv = nativemesh.uv;
        var uv2 = nativemesh.uv2;
        var uv3 = nativemesh.uv3;
        var uv4 = nativemesh.uv4;
        var uv5 = nativemesh.uv5;
        var uv6 = nativemesh.uv6;
        var uv7 = nativemesh.uv7;
        var uv8 = nativemesh.uv8;

        bakeoptions = new BakeOptions();

        bakeoptions.normals = normals.Length == nativemesh.vertexCount;
        bakeoptions.tangents = tangents.Length == nativemesh.vertexCount;
        bakeoptions.uv = uv.Length == nativemesh.vertexCount;
        bakeoptions.uv2 = uv2.Length == nativemesh.vertexCount;
        bakeoptions.uv3 = uv3.Length == nativemesh.vertexCount;
        bakeoptions.uv4 = uv4.Length == nativemesh.vertexCount;
        bakeoptions.uv5 = uv5.Length == nativemesh.vertexCount;
        bakeoptions.uv6 = uv6.Length == nativemesh.vertexCount;
        bakeoptions.uv7 = uv7.Length == nativemesh.vertexCount;
        bakeoptions.uv8 = uv8.Length == nativemesh.vertexCount;

        for (int i = 0; i < nativemesh.vertexCount; i++)
        {
            var vertex = new Vertex();

            vertex.position = positions[i];

            if (bakeoptions.normals)
                vertex.normal = normals[i];
            if (bakeoptions.tangents)
                vertex.tangent = tangents[i];
            if (bakeoptions.uv)
                vertex.uv = uv[i];
            if (bakeoptions.uv2)
                vertex.uv2 = uv2[i];
            if (bakeoptions.uv3)
                vertex.uv3 = uv3[i];
            if (bakeoptions.uv4)
                vertex.uv4 = uv4[i];
            if (bakeoptions.uv5)
                vertex.uv5 = uv5[i];
            if (bakeoptions.uv6)
                vertex.uv6 = uv6[i];
            if (bakeoptions.uv7)
                vertex.uv7 = uv7[i];
            if (bakeoptions.uv8)
                vertex.uv8 = uv8[i];

            vertices.Add(vertex);
        }

        var nodes = new int[vertices.Count];
        for (int i = 0; i < nodes.Length; i++)
        {
            nodes[i] = i;
        }

        List<int> indices = new List<int>();
        List<int> submeshids = new List<int>();

        for (int i = 0; i < nativemesh.subMeshCount; i++)
        {
            var triangles = nativemesh.GetTriangles(i);
            indices.AddRange(triangles);
            submeshids.AddRange(Enumerable.Repeat(i, triangles.Length));
        }

        Build(indices.ToArray(), submeshids.ToArray(), vertices.ToArray(), nodes);
    }

    public void Build(int[] indices, int[] indexsubmeshes, Vertex[] vertices, int[] vertexnodes)
    {
        this.vertices.AddRange(vertices);

        var numtriangles = indices.Length / 3;

        for (int i = 0; i < numtriangles; i++)
        {
            var edge0 = new Edge();
            edge0.submesh = indexsubmeshes[(i * 3) + 0];
            var edge0vertex = indices[(i * 3) + 0];
            edge0.vertex = vertices[edge0vertex];
            edge0.node = vertexnodes[edge0vertex];
            edge0.vertex.node = edge0.node;         // just in case the caller didn't set this

            var edge1 = new Edge();
            edge1.submesh = indexsubmeshes[(i * 3) + 1];
            var edge1vertex = indices[(i * 3) + 1];
            edge1.vertex = vertices[edge1vertex];
            edge1.node = vertexnodes[edge1vertex];
            edge1.vertex.node = edge1.node;

            var edge2 = new Edge();
            edge2.submesh = indexsubmeshes[(i * 3) + 2];
            var edge2vertex = indices[(i * 3) + 2];
            edge2.vertex = vertices[edge2vertex];
            edge2.node = vertexnodes[edge2vertex];
            edge2.vertex.node = edge2.node;

            edge0.next = edge1;
            edge1.next = edge2;
            edge2.next = edge0;

            edges.Add(edge0);
            edges.Add(edge1);
            edges.Add(edge2);
        }

        FindOppositeEdges();

        nodes = vertexnodes.Max();
    }

    public void FindOppositeEdges()
    {
        // as the supernode indices are 32 bit, we can create perfect hashes for each edge (remember, edges are directional!)

        Dictionary<long, Edge> hashtable = new Dictionary<long, Edge>();

        foreach (var edge in edges)
        {
            hashtable.Add(edge.ForwardHash, edge);
        }

        foreach (var edge in edges)
        {
            if(hashtable.ContainsKey(edge.OppositeHash))
            {
                edge.opposite1 = hashtable[edge.OppositeHash];
            }
        }
    }

    public void Bisect(Edge edge)
    {
        // cut the edge in two.

        // edge1, 2 & 3 are just shorthand for within this function; since the edge list is cyclical, the entry edge is arbitrary.

        var edge2 = edge;
        var edge3 = edge.next;
        var edge1 = edge3.next;

        // bisecting can work one of two ways - resolve a non-conforming edge, or turn a previously conforming edge into a non-conforming one.

        int newnode = -1;

        var edgeA = new Edge();
        var edgeB = new Edge();
        var edgeCa = new Edge();
        var edgeCb = new Edge();

        if (edge.Conforming)
        {
            newnode = ++nodes;
        }
        else
        {
            newnode = edge.opposite2.node;
        }

        edgeA.node = edge2.node;
        edgeB.node = newnode;
        edgeCa.node = newnode;
        edgeCb.node = edge1.node;

        // create the per-triangle vertices for the new edge

        Vertex v0 = Vertex.Interpolate(edge2.vertex, edge3.vertex);

        vertices.Add(v0);
       
        edgeA.vertex = edge2.vertex;
        edgeCa.vertex = v0;
        edgeB.vertex = v0;
        edgeCb.vertex = edge1.vertex;

        v0.node = edgeCa.node;

        edgeA.submesh = edge2.submesh;
        edgeB.submesh = edge2.submesh;
        edgeCa.submesh = edge2.submesh;
        edgeCb.submesh = edge2.submesh;

        // now assign the pointers to create triangles out of these new edges

        edge1.next = edgeA;
        edgeA.next = edgeCa;
        edgeCa.next = edge1;

        edgeCb.next = edgeB;
        edgeB.next = edge3;
        edge3.next = edgeCb;

        // now assign the opposites

        edgeCa.opposite1 = edgeCb;
        edgeCb.opposite1 = edgeCa;

        if (edge2.Conforming)
        {
            if (edge2.opposite1 != null)
            {
                // remember the edge direction (i.e. winding order) will be reversed for the opposite edge
                edge2.opposite1.opposite1 = edgeB;
                edge2.opposite1.opposite2 = edgeA;

                edgeA.opposite1 = edge2.opposite1;
                edgeB.opposite1 = edge2.opposite1;
            }
        }
        else
        {
            if (edge2.opposite1 != null)
            {
                edge2.opposite1.opposite1 = edgeA;
                edgeA.opposite1 = edge2.opposite1;
            }

            if (edge2.opposite2 != null)
            {
                edge2.opposite2.opposite1 = edgeB;
                edgeB.opposite1 = edge2.opposite2;
            }
        }

        // remove the triangle entirely and readd the new ones at the end to keep the edge list structured

        edges.Remove(edge1);
        edges.Remove(edge2);
        edges.Remove(edge3);

        edges.Add(edge1);
        edges.Add(edgeA);
        edges.Add(edgeCa);
        edges.Add(edgeCb);
        edges.Add(edgeB);
        edges.Add(edge3);
    }

    public IEnumerable<Edge> FindNonConforming()
    {
        foreach (var edge in edges)
        {
            if(!edge.Conforming)
            {
                yield return edge;
            }
        }
    }

    public void BakeMesh(Mesh nativemesh)
    {
        Dictionary<int, List<int>> triangles = new Dictionary<int, List<int>>();

        foreach (var edge in edges)
        {
            if (!triangles.ContainsKey(edge.submesh))
            {
                triangles.Add(edge.submesh, new List<int>());
            }
        }

        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i].index = i;
        }

        for (int i = 0; i < edges.Count; i += 3)
        {
            var edge = edges[i];
            var triangleslist = triangles[edge.submesh];
            triangleslist.Add(edge.vertex.index);
            triangleslist.Add(edge.next.vertex.index);
            triangleslist.Add(edge.next.next.vertex.index);
        }

        nativemesh.vertices = vertices.Select(v => v.position).ToArray();
        if (bakeoptions.normals)
            nativemesh.normals = vertices.Select(v => v.normal).ToArray();
        if (bakeoptions.tangents)
            nativemesh.tangents = vertices.Select(v => v.tangent).ToArray();
        if (bakeoptions.uv)
            nativemesh.uv = vertices.Select(v => v.uv).ToArray();
        if (bakeoptions.uv2)
            nativemesh.uv2 = vertices.Select(v => v.uv2).ToArray();
        if (bakeoptions.uv3)
            nativemesh.uv3 = vertices.Select(v => v.uv3).ToArray();
        if (bakeoptions.uv4)
            nativemesh.uv4 = vertices.Select(v => v.uv4).ToArray();
        if (bakeoptions.uv5)
            nativemesh.uv5 = vertices.Select(v => v.uv5).ToArray();
        if (bakeoptions.uv6)
            nativemesh.uv6 = vertices.Select(v => v.uv6).ToArray();
        if (bakeoptions.uv7)
            nativemesh.uv7 = vertices.Select(v => v.uv7).ToArray();
        if (bakeoptions.uv8)
            nativemesh.uv8 = vertices.Select(v => v.uv8).ToArray();

        nativemesh.subMeshCount = triangles.Count;
        foreach (var submesh in triangles)
        {
            nativemesh.SetTriangles(submesh.Value, submesh.Key);
        }
    }

    public void RefineMesh(float edgeLength)
    {
        List<Edge> nonconformingedges = new List<Edge>();

        do
        {
            Edge longest = edges.First();
            foreach (var edge in edges)
            {
                if (longest.Length < edge.Length)
                {
                    longest = edge;
                }
            }

            if (longest.Length > edgeLength)
            {
                Bisect(longest);
            }
            else
            {
                break;
            }

            do
            {
                foreach (var item in nonconformingedges)
                {
                    Bisect(item);
                }
                nonconformingedges.Clear();
                nonconformingedges.AddRange(FindNonConforming());
            } while (nonconformingedges.Count > 0);

        } while (true);
    }
}

