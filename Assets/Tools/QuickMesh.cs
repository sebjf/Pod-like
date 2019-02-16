using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class LinqExtensions
{
    public static void Replace<T>(this T[] array, T replace, T with) where T : class
    {
        for (int i = 0; i < array.Length; i++)
        {
            if(array[i] == replace)
            {
                array[i] = with;
            }
        }
    }
}

public class SpatialHash<T>
{
    public SpatialHash(float cellsize, int tablesize)
    {
        d = cellsize;
        n = tablesize;
    }

    private readonly float d;
    private readonly int n;

    //Teschner, M., Heidelberger, B., Müller, M., Pomeranets, D., & Gross, M. (2003). 
    //Optimized Spatial Hashing for Collision Detection of Deformable Objects. 
    //In Proceedings of Vision, Modeling, Visualization VMV’03 (pp. 47--54). http://doi.org/10.1.1.4.5881
    public int Hash(Vector3 v)
    {
        return ((Mathf.FloorToInt(v.x / d) * 73856093) ^
                (Mathf.FloorToInt(v.y / d) * 19349663) ^
                (Mathf.FloorToInt(v.z / d) * 83492791)) % n;
    }

    public Dictionary<int, List<T>> HashTable = new Dictionary<int, List<T>>();

    public void Add(Vector3 v, T i)
    {
        int h = Hash(v);
        if (!HashTable.ContainsKey(h))
        {
            HashTable.Add(h, new List<T>());
        }
        HashTable[h].Add(i);
    }
}

public class QuickMesh
{
    public class Vertex
    {
        public int index;

        public List<int> originalIndices;

        public Vector3 position;
        public List<Triangle> triangles;

        public Vector3 angleWeightedPsuedoNormal;

        public Vertex(int originalindex, Vector3 position)
        {
            this.position = position;
            originalIndices = new List<int>();
            originalIndices.Add(originalindex);
            triangles = new List<Triangle>();
        }
    }

    public class Triangle
    {
        public int submesh;
        public Vertex[] vertices;

        public Vector3 normal;

        public float Angle(Vertex v)
        {
            int index = -1;
            for(int i = 0; i < vertices.Length; i++)
            {
                if(vertices[i] == v)
                {
                    index = i;
                }
            }
            int other1 = (index + 1) % 3;
            int other2 = (index + 2) % 3;

            var e1 = vertices[other1].position - vertices[index].position;
            var e2 = vertices[other2].position - vertices[index].position;

            return Vector3.Dot(e1.normalized, e2.normalized);
        }

        public Triangle(int submesh, params Vertex[] vertices) // this is always of length three. its defined as params simply to make the constructor call nicer.
        {
            this.submesh = submesh;
            this.vertices = vertices;
        }
    }

    public List<Vertex> vertices = new List<Vertex>();
    public List<Triangle> triangles = new List<Triangle>();

    public ulong[] vertexSubmeshAssociationMap;    // up to 64 submeshes

    public IEnumerable<Vector3> positions
    {
        get
        {
            foreach (var item in vertices)
            {
                yield return item.position;
            }
        }
    }

    public QuickMesh(Vector3[] baked)
    {
        for (int i = 0; i < baked.Length; i++)
        {
            vertices.Add(new Vertex(i, baked[i]));
        }
        UpdateVertexIndices();
    }

    public QuickMesh(Mesh mesh, Transform transform)
        :this(mesh.vertices.Select(v => transform.TransformPoint(v)).ToArray(), mesh)
    {
    }

    public QuickMesh(Vector3[] baked, Mesh triangles)
    {
        for (int i = 0; i < baked.Length; i++)
        {
            vertices.Add(new Vertex(i, baked[i]));
        }

        for (int submesh = 0; submesh < triangles.subMeshCount; submesh++)
        {
            var indices = triangles.GetTriangles(submesh);

            int numIndices = indices.Length;
            int numTriangles = numIndices / 3;

            for (int i = 0; i < numTriangles; i++)
            {
                this.triangles.Add(new Triangle(
                    submesh,
                    vertices[indices[(i * 3) + 0]],
                    vertices[indices[(i * 3) + 1]],
                    vertices[indices[(i * 3) + 2]]
                    ));
            }
        }

        UpdateVertexIndices();
        UpdateVertexReferences();
    }

    public void UpdateVertexReferences()
    {
        foreach (var vertex in vertices)
        {
            vertex.triangles.Clear();
        }

        foreach (var triangle in triangles)
        {
            foreach (var vertex in triangle.vertices)
            {
                vertex.triangles.Add(triangle);
            }
        }
    }

    private void UpdateVertexIndices()
    {
        for(int i = 0; i < vertices.Count; i++)
        {
            vertices[i].index = i;
        }
    }

    public void UpdateNormals()
    {
        foreach (var triangle in triangles)
        {
            triangle.normal = Vector3.Cross(triangle.vertices[1].position - triangle.vertices[0].position, triangle.vertices[2].position - triangle.vertices[0].position).normalized;
        }

        foreach (var vertex in vertices)
        {
            var normal = Vector3.zero;
            foreach (var triangle in vertex.triangles)
            {
                normal += (triangle.normal * triangle.Angle(vertex));
            }
            vertex.angleWeightedPsuedoNormal = normal.normalized;
        }
    }

    public void WeldVertices(float threshold)
    {
        var hash = new SpatialHash<Vertex>(threshold, 10000000);
        vertices.ForEach((v) => { hash.Add(v.position, v); });

        var tocombine = new List<Vertex>();
        var combined = new List<Vertex>();

        foreach (var cell in hash.HashTable.Values)
        {
            // check if the vertices are actually colocated or are just hashed similarly
            while(cell.Count > 0)
            {
                var next = cell.First();
                cell.Remove(next);

                tocombine.Clear();

                for (int i = 0; i < cell.Count; i++)
                {
                    if((next.position - cell[i].position).magnitude < threshold)
                    {
                        tocombine.Add(cell[i]);
                    }
                }

                // combine the vertices

                foreach (var vertex in tocombine)
                {
                    foreach (var triangle in vertex.triangles)
                    {
                        triangle.vertices.Replace(vertex, next);
                    }

                    next.originalIndices.AddRange(vertex.originalIndices);

                    cell.Remove(vertex);
                }

                combined.Add(next);
            }
        }

        vertices = combined;

        UpdateVertexReferences();
    }

    public void Filter(Bounds bounds, Transform boundstransform)
    {
        UpdateVertexReferences();
        var toremove = vertices.Where(v => !bounds.Contains(boundstransform.InverseTransformPoint(v.position))).ToList();

        foreach (var item in toremove)
        {
            foreach (var triangle in item.triangles)
            {
                triangles.Remove(triangle);
            }

            vertices.Remove(item);
        }
    }

}
