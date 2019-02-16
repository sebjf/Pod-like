using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


public class TrackTools : EditorWindow
{
    [MenuItem("Tools/Track Tools")]
    static void Init()
    {
        var window = (TrackTools)EditorWindow.GetWindow(typeof(TrackTools));
        window.Show();
    }

    private Material replace;
    private Material with;

    void OnGUI()
    {


        if (GUILayout.Button("Replace Materials"))
        {
        }
    }

}

