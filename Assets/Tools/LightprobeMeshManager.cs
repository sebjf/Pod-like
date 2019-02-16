using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LightprobeMeshManager : LightprobeManager
{
    public override void UpdateSamples()
    {
        var meshtransform = this.mesh.transform;
        var mesh = this.mesh.sharedMesh;
        var uvs = mesh.uv;

        QuickMesh quickmesh = new QuickMesh(mesh, meshtransform);
        quickmesh.Filter(bounds, transform);
        quickmesh.WeldVertices(threshold);
        quickmesh.UpdateNormals();

        superprobes = new List<Vector3>();

        foreach (var item in quickmesh.vertices)
        {
            superprobes.Add(item.position + item.angleWeightedPsuedoNormal * surfaceoffset);
        }

        base.UpdateSamples();
    }
}
