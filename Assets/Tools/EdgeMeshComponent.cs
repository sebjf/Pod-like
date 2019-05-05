using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EdgeMeshComponent : MonoBehaviour
{
    public EdgeMesh Mesh;
    public Mesh Mirror;

    public bool annotateEdges;

    public void Reset()
    {
        Mesh = new EdgeMesh();
        Mesh.Build(GetComponentInChildren<MeshFilter>().sharedMesh);  
    }
}
