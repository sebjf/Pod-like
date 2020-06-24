using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.TerrainAPI;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(TrainingManager))]
public class TrainingManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Available Scenes:");
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            EditorGUILayout.LabelField(System.IO.Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i))); // Work around Unity bug https://forum.unity.com/threads/getscenebybuildindex-problem.452560/
        }
    }
}
