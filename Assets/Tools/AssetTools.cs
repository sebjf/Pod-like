using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AssetDirectories
{
    public string name;
    public string directory;
    public string assetpath;
    public string filename;
    public string file;
}

public static class AssetTools
{
    public static AssetDirectories FindAssetPaths(GameObject asset)
    {
        AssetDirectories paths = new AssetDirectories();
        var mesh = asset.GetComponentInChildren<MeshFilter>().sharedMesh;
        paths.name = asset.name;
        paths.assetpath = AssetDatabase.GetAssetPath(mesh);
        paths.directory = Path.GetDirectoryName(paths.assetpath);
        paths.filename = asset.name; // Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(mesh));
        paths.file = Path.Combine(paths.directory, paths.filename);
        return paths;
    }

    public static string FindAssetMetadataFilename(GameObject asset)
    {
        var paths = FindAssetPaths(asset);
        var metadatafile = Path.Combine(Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - "Assets\\".Length), paths.directory), paths.filename + ".xml");
        return metadatafile;
    }

    public static byte[] FindAssetMetadataBytes(GameObject asset)
    {
        var metadatafile = FindAssetMetadataFilename(asset);
        var metadatabytes = File.ReadAllBytes(metadatafile);
        return metadatabytes;
    }
}
