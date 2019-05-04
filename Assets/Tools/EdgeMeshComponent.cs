using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EdgeMeshComponent : MonoBehaviour
{
    [HideInInspector]
    public EdgeMesh Mesh;

    private void Reset()
    {
        Mesh = new EdgeMesh();
        Mesh.Build(GetComponent<MeshFilter>().sharedMesh);
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
