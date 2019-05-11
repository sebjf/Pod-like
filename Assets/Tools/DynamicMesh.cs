using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMesh : MonoBehaviour
{
    Mesh mesh;
    Vector3[] positions;
    Color[] uv3;
    DeformationModel deformer;

    public AnimationCurve blendFunction;

    public bool forceUpdate;

    public enum BlendChannel
    {
        Strain,
        Deformation,
        Distance
    }

    public BlendChannel blendChannel;


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
        if (deformer.lastImpactFrame == Time.frameCount || forceUpdate)    // lastImpactFrame is set in OnCollisionEnter, which always occurs before Update
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = deformer.mesh.nodes[deformer.nodesmap[i]].position;

                switch (blendChannel)
                {
                    case BlendChannel.Strain:
                        uv3[i].r = 1 - blendFunction.Evaluate(deformer.mesh.nodes[deformer.nodesmap[i]].y);
                        break;
                    case BlendChannel.Deformation:
                        uv3[i].r = 1 - blendFunction.Evaluate((positions[i] - deformer.mesh.nodes[deformer.nodesmap[i]].origin).magnitude / deformer.maxd);
                        break;
                    case BlendChannel.Distance:
                        uv3[i].r = 1 - (deformer.mesh.nodes[deformer.nodesmap[i]].d / deformer.geodesicmetric);
                        break;
                    default:
                        break;
                }                
            }
            mesh.vertices = positions;
            mesh.colors = uv3;
        }
    }
}
