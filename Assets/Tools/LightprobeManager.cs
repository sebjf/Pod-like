using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LightprobeManager : MonoBehaviour
{
    public Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
    public MeshFilter mesh;

    public float threshold = 1f;
    public float surfaceoffset = 0.1f;

    public List<Vector3> superprobes;

    public virtual void UpdateSamples()
    {
        LightProbeGroup lightProbeGroup = GetComponent<LightProbeGroup>();
        lightProbeGroup.probePositions = superprobes.Select(x => transform.InverseTransformPoint(x)).ToArray();

        foreach(var manager in FindObjectsOfType<LightprobeManager>())
        {
            manager.OnLightProbesUpdate();
        }
    }

    public virtual void OnLightProbesUpdate()
    {
    }
}
