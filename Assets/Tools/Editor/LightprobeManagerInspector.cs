using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(LightprobeManager),true)]
public class LightprobeManagerInspector : Editor
{
    private BoxBoundsHandle m_BoundsHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        var component = target as LightprobeManager;

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("bounds"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mesh"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("threshold"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("surfaceoffset"));

        if (component is LightprobeTextureManager)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("controlTextures"), true);
        }

        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Update"))
        {
            component.UpdateSamples();
        }
    }

    private void OnSceneGUI()
    {
        LightprobeManager component = (LightprobeManager)target;

        Handles.matrix = component.transform.localToWorldMatrix;

        // copy the target object's data to the handle
        m_BoundsHandle.center = component.bounds.center;

        m_BoundsHandle.size = component.bounds.size;

        // draw the handle
        EditorGUI.BeginChangeCheck();
        m_BoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            // record the target object before setting new values so changes can be undone/redone
            Undo.RecordObject(component, "Change Bounds");

            // copy the handle's updated data back to the target object
            Bounds newBounds = new Bounds();
            newBounds.center = m_BoundsHandle.center;
            newBounds.size = m_BoundsHandle.size;
            component.bounds = newBounds;
        }
    }

    [DrawGizmo(GizmoType.Selected)]
    static void DrawGizmoForMyScript(LightprobeManager component, GizmoType gizmoType)
    {
        if (component.superprobes != null)
        {
            foreach (var item in component.superprobes)
            {
                Gizmos.DrawWireSphere(item, component.threshold);
            }
        }
    }
}
