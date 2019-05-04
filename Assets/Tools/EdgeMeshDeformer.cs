using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(EdgeMeshComponent))]
public class EdgeMeshDeformer : MonoBehaviour
{
    public int edgeid;

    public EdgeMesh mesh
    {
        get
        {
            return GetComponent<EdgeMeshComponent>().Mesh;
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

    public void Refine(int edge)
    {
        mesh.Bisect(mesh.edges[edge]);  
    }

    public void Correct()
    {
        List<EdgeMesh.Edge> nonconformingedges = new List<EdgeMesh.Edge>();
        do
        {
            foreach (var item in nonconformingedges)
            {
                mesh.Bisect(item);
            }
            nonconformingedges.Clear();
            nonconformingedges.AddRange(mesh.FindNonConforming());
        } while (nonconformingedges.Count > 0);
    }
}
