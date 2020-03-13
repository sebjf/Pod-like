using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GraphWindow : EditorWindow
{
    private GraphOverlay graph;

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Graph")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        GraphWindow window = (GraphWindow)EditorWindow.GetWindow(typeof(GraphWindow));
        window.Show();
    }

    void OnGUI()
    {
        if (graph)
        {
            EditorGUI.DrawTextureTransparent(new Rect(Vector2.zero, this.position.size), graph.GetComponent<Camera>().targetTexture);
        }
    }

    private void Update()
    {
        if(graph == null)
        {
            graph = FindObjectOfType<GraphOverlay>();
        }

        if (graph != null)
        {
            if (graph.camera != null)
            {
                var texture = graph.camera.targetTexture;
                if (texture.width != position.width || texture.height != position.height)
                {
                    texture.Release();
                    texture = new RenderTexture((int)position.width, (int)position.height, 24);
                    graph.camera.targetTexture = texture;
                }
            }

            Repaint();
        }
    }
}
