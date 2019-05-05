using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMesh : MonoBehaviour
{
    Mesh mesh;
    Vector3[] positions;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        positions = new Vector3[mesh.vertexCount];
    }

    // Update is called once per frame
    void Update()
    {
        var deformer = GetComponentInParent<DeformationModel>();

        for(int i = 0; i < positions.Length; i++)
        {
            positions[i] = deformer.mesh.nodes[deformer.nodesmap[i]].position;
        }

        mesh.vertices = positions;
    }
}
