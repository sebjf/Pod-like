using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class EdgeMesh
{
    public EdgeMesh()
    {
        vertices = new List<Vertex>();
        edges = new List<Edge>();
        nodes = new List<Node>();
    }

    [Serializable]
    public class Node
    {
    }

    [Serializable]
    public class Vertex
    {
        public Vector3 position;
    }

    [Serializable]
    public class Edge
    {
        public int node;
        public int vertex;
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
    }

    public List<Vertex> vertices;
    public List<Edge> edges;
    public List<Node> nodes;

    public void Build(Mesh nativemesh)
    {
        vertices = new List<Vertex>();

        var positions = nativemesh.vertices;
        foreach (var vertex in positions)
        {
            vertices.Add(new Vertex()
            {
                position = vertex
            });
            nodes.Add(new Node());
        }

        var indices = nativemesh.triangles;
        var numtriangles = indices.Length / 3;

        for (int i = 0; i < numtriangles; i++)
        {
            var edge0 = new Edge();
            edge0.vertex = indices[(i * 3) + 0];
            edge0.node = edge0.vertex;  // for now the vertex is the same as the node

            var edge1 = new Edge();
            edge1.vertex = indices[(i * 3) + 1];
            edge1.node = edge1.vertex;

            var edge2 = new Edge();
            edge2.vertex = indices[(i * 3) + 2];
            edge2.node = edge2.vertex;

            edge0.next = edge1;
            edge1.next = edge2;
            edge2.next = edge0;

            edges.Add(edge0);
            edges.Add(edge1);
            edges.Add(edge2);
        }

        FindOppositeEdges();
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
            newnode = nodes.Count;
            nodes.Add(new Node());
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

        var v0 = InterpolateVertex(edge2.vertex, edge3.vertex);
        var v1 = InterpolateVertex(edge2.vertex, edge3.vertex);

        edgeA.vertex = edge2.vertex;
        edgeCa.vertex = v0;
        edgeB.vertex = v1;
        edgeCb.vertex = edge1.vertex;

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

        edges.Add(edgeA);
        edges.Add(edgeB);
        edges.Add(edgeCa);
        edges.Add(edgeCb);

        edges.Remove(edge2);
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

    public int InterpolateVertex(int i0, int i1)
    {
        int i = vertices.Count;
        var v0 = vertices[i0];
        var v1 = vertices[i1];
        var v = new Vertex();
        v.position = Vector3.Lerp(v0.position, v1.position, 0.5f);
        vertices.Add(v);
        return i;
    }

    public void Bake(Mesh existing)
    {
        existing.vertices = vertices.Select(v => v.position).ToArray();
        existing.triangles = edges.Select(e => e.vertex).ToArray();
    }
}

