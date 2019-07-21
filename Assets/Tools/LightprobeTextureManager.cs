using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class LightprobeTexture
{
    public int submeshid;
    public Texture2D texture;
    public float height;
}

[Serializable]
public class LightprobeController
{
    public int submesh;
    public Vector2 uv;
    public float height;
    public float multiplier;
}

[Serializable]
public class LightProbeModifier
{
    public Vector3 probe; // when the probes are given to unity the positions shift a little, so we identify by closest point
    public int globalindex;
    public float scalar;
}

public class LightprobeTextureManager : LightprobeManager
{
    public List<LightprobeTexture> controlTextures;
    public List<LightprobeController> controllers;

    public List<LightProbeModifier> bakedProbeModifiers;

    private void Start()
    {
        ApplyLightmapModifiers();       
    }

    public override void OnLightProbesUpdate()
    {
        //update the probe global indices

        Vector3[] probePositions = LightmapSettings.lightProbes.positions;

        // filter the probes to speed up search
        QuickMesh mesh = new QuickMesh(probePositions);
        mesh.Filter(bounds, transform);

        foreach (var modifier in bakedProbeModifiers)
        {
            int minindex = -1;
            float mindistance = float.PositiveInfinity;
            for (int i = 0; i < probePositions.Length; i++)
            {
                var distance = (probePositions[i] - modifier.probe).magnitude;
                if (distance < mindistance)
                {
                    minindex = i;
                    mindistance = distance;
                }
            }

            modifier.globalindex = minindex;
        }
    }

    private void ApplyLightmapModifiers()
    {
        // https://docs.unity3d.com/ScriptReference/LightProbes-bakedProbes.html

        SphericalHarmonicsL2[] bakedProbes = LightmapSettings.lightProbes.bakedProbes;

        foreach (var modifier in bakedProbeModifiers)
        {
            bakedProbes[modifier.globalindex] *= modifier.scalar;
        }

        LightmapSettings.lightProbes.bakedProbes = bakedProbes;
    }

#if UNITY_EDITOR
    public override void UpdateSamples()
    {
        if (controllers == null)
        {
            controllers = new List<LightprobeController>();
        }
        if (controlTextures == null)
        {
            controlTextures = new List<LightprobeTexture>();
        }
        if(bakedProbeModifiers == null)
        {
            bakedProbeModifiers = new List<LightProbeModifier>();
        }

        UpdateTextureControllers();

        var meshtransform = this.mesh.transform;
        var mesh = this.mesh.sharedMesh;
        var uvs = mesh.uv;

        QuickMesh quickmesh = new QuickMesh(mesh, meshtransform);
        quickmesh.Filter(bounds, transform);
        quickmesh.UpdateNormals();

        List<Vector3> spawnprobes = new List<Vector3>();
        List<float> multipliers = new List<float>();

        var activeSubmeshes = controllers.Select(c => c.submesh).Distinct().ToArray();
        foreach (var submesh in activeSubmeshes)
        {
            var controllers = this.controllers.Where(c => c.submesh == submesh).ToList();

            foreach (var triangle in quickmesh.triangles.Where(t => t.submesh == submesh))
            {
                foreach (var controller in controllers)
                {
                    // for each triangle in the mesh, get the barycentric coordinates of the probe spawn point
                    // the 'world' coordinate system is the texture (uv) space

                    // at this stage, the vertex indices match the originals
                    var barycentric = Barycentric(controller.uv, uvs[triangle.vertices[0].index], uvs[triangle.vertices[1].index], uvs[triangle.vertices[2].index]);
                    var spawn = triangle.vertices[0].position * barycentric.x + triangle.vertices[1].position * barycentric.y + triangle.vertices[2].position * barycentric.z;

                    if (barycentric.x > 0 && barycentric.y > 0 && barycentric.x + barycentric.y < 1)
                    {
                        spawnprobes.Add(spawn + triangle.normal * controller.height);
                        multipliers.Add(controller.multiplier);
                    }
                }
            }
        }

        // remove redundant colocated probes
        QuickMesh probes = new QuickMesh(spawnprobes.ToArray());
        probes.WeldVertices(threshold);

        bakedProbeModifiers.Clear();

        foreach (var item in probes.vertices)
        {
            bakedProbeModifiers.Add(new LightProbeModifier()
            {
                probe = item.position,
                scalar = multipliers[item.index]
            });
        }

        superprobes = bakedProbeModifiers.Select(x => x.probe).ToList();
        base.UpdateSamples();

        // apply modifiers immediately so we can see the result. any rebaking will change this at design time, but it will be reset in start().
        ApplyLightmapModifiers();
    }

    private void UpdateTextureControllers()
    {
        controllers.Clear();

        foreach (var item in controlTextures)
        {
            SetTextureReadable(item.texture);

            // search the texture to build controllers

            for (int x = 0; x < item.texture.width; x++)
            {
                for (int y = 0; y < item.texture.height; y++)
                {
                    var colour = item.texture.GetPixel(x, y);

                    // parse the control colour
                    if (colour.r >= 1)
                    {
                        controllers.Add(new LightprobeController()
                        {
                            submesh = item.submeshid,
                            uv = new Vector2((float)x / (float)item.texture.width, (float)y / (float)item.texture.height),
                            height = (colour.g * item.height),
                            multiplier = colour.b
                        });
                    }
                }
            }
        }
    }

    // thanks: https://stackoverflow.com/questions/25175864/making-a-texture2d-readable-in-unity-via-code
    private static void SetTextureReadable(Texture2D texture)
    {
        string assetPath = AssetDatabase.GetAssetPath(texture);
        var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (tImporter != null)
        {
            tImporter.isReadable = true;
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.Refresh();
        }
    }

    private static Vector3 Barycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        Vector2 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        var v = (d11 * d20 - d01 * d21) / denom;
        var w = (d00 * d21 - d01 * d20) / denom;
        var u = 1.0f - v - w;
        return new Vector3(u, v, w);
    }
#endif
}
