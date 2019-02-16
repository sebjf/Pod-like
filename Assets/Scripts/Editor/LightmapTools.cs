using UnityEngine;

using UnityEditor;

// https://support.unity3d.com/hc/en-us/articles/214718843-My-Emissive-material-shader-does-not-appear-in-the-Lightmap-

public class LightmapTools : MonoBehaviour
{
    [MenuItem("Lightmapping/SetEmissiveFlag")]
    static void SetEmissionFlags()
    {
        foreach (var item in Selection.objects)
        {
            if(item is Material)
            {
                SetEmissionFlag(item as Material);
            }
            if(item is Renderer)
            {
                SetEmissionFlag(item as Renderer);
            }
            if(item is GameObject)
            {
                SetEmissionFlag(item as GameObject);
            }
        }
    }

    public static void SetEmissionFlag(GameObject obj)
    {
        foreach (var item in obj.GetComponents<Renderer>())
        {
            SetEmissionFlag(item);
        }
    }

    public static void SetEmissionFlag(Renderer renderer)
    {
        foreach (var item in renderer.sharedMaterials)
        {
            SetEmissionFlag(item);
        }
    }

    public static void SetEmissionFlag(Material material)
    {
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; //Only this for now, or else manually clear isBlack
    }
}