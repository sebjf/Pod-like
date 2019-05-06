using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMesh : MonoBehaviour
{
    Mesh mesh;
    Vector3[] positions;
    Color[] uv3;
    DeformationModel deformer;

    private void Awake()
    {
        deformer = GetComponentInParent<DeformationModel>();
    }

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        positions = new Vector3[mesh.vertexCount];
        uv3 = new Color[mesh.vertexCount];
    }

    // Update is called once per frame
    void Update()
    {
        if (deformer.lastImpactFrame == Time.frameCount)    // lastImpactFrame is set in OnCollisionEnter, which always occurs before Update
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = deformer.mesh.nodes[deformer.nodesmap[i]].position;
                 uv3[i].r = 1 - (positions[i] - deformer.mesh.nodes[deformer.nodesmap[i]].origin).magnitude;
                //uv3[i].r = 1 - deformer.mesh.nodes[deformer.nodesmap[i]].y;
            }
            mesh.vertices = positions;
            mesh.colors = uv3;
        }
    }
}
