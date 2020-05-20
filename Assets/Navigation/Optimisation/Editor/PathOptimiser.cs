using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PathOptimiser 
{
    //https://answers.unity.com/questions/949102/how-to-add-context-menu-on-assets-and-asset-folder.html

    [MenuItem("Assets/Optimise Path")]
    private static void PathOptimiserToolMenu()
    {
        var window = (PathOptimiserWindow)EditorWindow.GetWindow(typeof(PathOptimiserWindow));
        window.sections = Selection.activeObject;
    }

    [MenuItem("Assets/Optimise Path", true)]
    private static bool PathOptimiserToolValidation()
    {
        // This returns true when the selected object is a Variable (the menu item will be disabled otherwise).
        try
        {
            return AssetDatabase.GetAssetPath(Selection.activeObject).EndsWith("txt");
        }
        catch
        {
            return false;
        }
    }

    public static void InitWindow()
    {

    }

}

public class PathOptimiserWindow : EditorWindow
{
    public Object sections;
    public int iterationsMcp = 50000;
    public int iterationsSp = 10000;

    private string processoutput = "";
    private Process process;

    private void OnGUI()
    {
        sections = EditorGUILayout.ObjectField("Sections", sections, typeof(Object), false);

        int substring = "Assets/".Length;

        var tool = "";
        var guids = AssetDatabase.FindAssets("PathOptimiser");
        foreach (var item in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(item);
            if (path.EndsWith("exe"))
            {
                tool = Path.Combine(Application.dataPath, path.Substring(substring));
                break;
            }
        }

        if (tool.Length <= 0)
        {
            EditorGUILayout.HelpBox("Could not find PathOptimiser.exe", MessageType.Error); // https://docs.unity3d.com/ScriptReference/EditorGUILayout.HelpBox.html
            return;
        }

        var sectionspath = AssetDatabase.GetAssetPath(sections);
        var sectionsfilename = Path.GetFileName(sectionspath);
        var directory = Path.Combine(Application.dataPath, Path.GetDirectoryName(sectionspath).Substring(substring));
        var inputpath = Path.Combine(directory, sectionsfilename);
        var mcppath = Path.Combine(directory, "minimumcurvaturepath.txt");
        var sppath = Path.Combine(directory, "shortestpath.txt");

        EditorStyles.label.wordWrap = true;
        EditorGUILayout.LabelField("Input", inputpath);
        EditorGUILayout.LabelField("MCP", mcppath);
        EditorGUILayout.LabelField("SP", sppath);

        iterationsMcp = EditorGUILayout.IntField("Iterations", iterationsMcp);

        if (GUILayout.Button("Minimum Curvature Path"))
        {
            StartTool(tool, inputpath, mcppath, iterationsMcp, 0);
        }

        iterationsSp = EditorGUILayout.IntField("Iterations", iterationsSp);

        if (GUILayout.Button("Shortest Path"))
        {
            StartTool(tool, inputpath, sppath, iterationsSp, 1);
        }

        EditorGUILayout.TextArea(processoutput, GUILayout.Height(150));

        GUILayout.FlexibleSpace(); // https://answers.unity.com/questions/1364141/positioning-a-button-at-the-bottom-of-an-editor-wi.html

        EditorGUILayout.LabelField("Tool", tool);
    }

    private void StartTool(string tool, string inpath, string outpath, int iterations, int mode)
    {
        processoutput = "";
        //https://www.technical-recipes.com/2016/how-to-run-processes-and-obtain-the-output-in-c/
        var startInfo = new ProcessStartInfo()
        {
            FileName = tool,
            Arguments = string.Format("-p \"{0}\" -o \"{1}\" -m {2} -i {3}", inpath, outpath, mode, iterations),
            UseShellExecute = true,
            CreateNoWindow = false
        };
        process = new Process();
        process.StartInfo = startInfo;
        process.Start();
    }
}

