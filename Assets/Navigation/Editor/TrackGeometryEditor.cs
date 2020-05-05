using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEditor;

[CustomEditor(typeof(TrackGeometry))]
public class TrackGeometryEditor : Editor
{
    public static bool createWaypoints;

    public override void OnInspectorGUI()
    {
        var component = target as TrackGeometry;

        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("curvatureSampleDistance"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.LabelField("Waypoints: " + component.waypoints.Count);
        EditorGUILayout.LabelField("Length: " + component.totalLength);

        EditorGUILayout.HelpBox("Press Shift to enter Waypoint Create Mode", MessageType.Info); // https://answers.unity.com/questions/1019430

        if (GUILayout.Button("Recompute"))
        {
            Undo.RecordObject(component, "Recompute");
            component.Recompute();
        }
    }

    public struct SelectionRectangle
    {
        public Vector2 start
        {
            set
            {
                a = value;
            }
        }

        public Vector2 end
        {
            set
            {
                b = value;
            }
        }

        public Vector2 min
        {
            get
            {
                return Vector2.Min(a, b);
            }
        }

        public Vector2 max
        {
            get
            {
                return Vector2.Max(a, b);
            }
        }

        public Vector2 size
        {
            get
            {
                return max - min;
            }
        }


        Vector2 a;
        Vector2 b;

        public Rect rect
        {
            get
            {
                return new Rect(min, size);
            }
        }
    }


    private SelectionRectangle selectionRectangle;
    private bool selectionBoxActive;

    private void OnSceneGUI()
    {
        var component = target as TrackGeometry;

        //prevent left click changing focus
        //https://answers.unity.com/questions/564457/intercepting-left-click-in-scene-view-for-custom-e.html

        if (Event.current.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(0);
        }

        if ((Event.current.modifiers & EventModifiers.Alt) != 0)
        {
            return; // in navigation mode
        }

        if((Event.current.modifiers & EventModifiers.Shift) != 0)
        {
            createWaypoints = true;
        }
        else
        {
            createWaypoints = false;
        }

        Undo.RecordObject(component, "Changed Track Geometry");

        // do the handles first so they will use the mouse events

        if ((Event.current.modifiers & EventModifiers.Control) <= 0)
        {
            foreach (var waypoint in component.selected)
            {
                waypoint.left = Handles.PositionHandle(waypoint.left, Quaternion.identity);
                waypoint.right = Handles.PositionHandle(waypoint.right, Quaternion.identity);
            }
        }

        component.Recompute();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            selectionRectangle.start = Event.current.mousePosition;
        }

        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            selectionRectangle.end = Event.current.mousePosition;
            selectionBoxActive = true;
        }

        // a little different from regular casting: https://forum.unity.com/threads/editor-camera-raycasting-cant-get-it-to-work.199474/
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit raycast;
        bool selectedPoint = Physics.Raycast(ray, out raycast);

        if (selectedPoint)
        {
            component.highlightedPoint = raycast.point;
        }
        else
        {
            component.highlightedPoint = null;
        }

        if (selectionBoxActive)
        {
            Color color = Color.gray;
            color.a = 0.5f;

            Handles.BeginGUI();
            Handles.DrawSolidRectangleWithOutline(selectionRectangle.rect, color, Color.white);
            Handles.EndGUI();

            // check highlight from box
            component.highlighted.Clear();
            foreach (var waypoint in component.waypoints)
            {
                if (selectionRectangle.rect.Contains(HandleUtility.WorldToGUIPoint(waypoint.position)))
                {
                    component.highlighted.Add(waypoint);
                }
            }
        }
        else
        {
            if (Event.current.type == EventType.MouseMove)
            {
                component.highlighted.Clear();
                component.highlighted.AddRange(component.Raycast(ray));
            }
        }

        if (Event.current.type == EventType.MouseUp && Event.current.button == 1)
        {
            component.selected.Clear();
        }

        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            selectionBoxActive = false;

            if (component.highlighted.Count > 0)   // if the user is hovering on an existing waypoint, or if they are clicking the scene to create a new one
            {
                component.selected.Clear();
                component.selected.AddRange(component.highlighted);
            }
            else
            {
                if (createWaypoints)
                {
                    if (selectedPoint)
                    {
                        if (component.lastSelected != null || component.waypoints.Count <= 0)
                        {
                            var wp = new TrackWaypoint()
                            {
                                up = raycast.normal,
                                left = raycast.point + Vector3.left,
                                right = raycast.point + Vector3.right
                            };

                            if (component.lastSelected != null)
                            {
                                wp.forward = (wp.position - component.lastSelected.position).normalized;
                                wp.width = 10;
                            }

                            component.Add(component.lastSelected, wp);

                            component.selected.Clear();
                            component.selected.Add(wp);
                        }
                    }
                }
            }

            Event.current.Use();
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
        {
            foreach (var item in component.selected)
            {
                component.waypoints.Remove(item);
            }
            component.Recompute();
            component.selected.Clear();
            Event.current.Use();
        }

        // update width
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W)
        {
            foreach (var item in component.selected)
            {
                FindWidth(component, item);
            }
            Event.current.Use();
        }

        // reset view
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
        {
            var average = Vector3.zero;
            foreach (var item in component.selected)
            {
                average += item.position;
            }
            average /= component.selected.Count;

            SceneView.lastActiveSceneView.LookAt(average);
            Event.current.Use();
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.T)
        {
            foreach (var item in component.selected)
            {
                FindTangent(component, item);
            }
            Event.current.Use();
        }

        serializedObject.Update();
        serializedObject.ApplyModifiedProperties();

        SceneView.RepaintAll();
    }

    private void FindWidth(TrackGeometry component, TrackWaypoint waypoint)
    {
        RaycastHit raycast;
        if (Physics.Raycast(new Ray(waypoint.position + waypoint.up * 0.01f, -waypoint.tangent), out raycast))
        {
            if (raycast.distance < 100f)
            {
                waypoint.left = raycast.point;
            }
        }
        if (Physics.Raycast(new Ray(waypoint.position + waypoint.up * 0.01f, waypoint.tangent), out raycast))
        {
            if (raycast.distance < 100f)
            {
                waypoint.right = raycast.point;
            }
        }
    }

    private void FindTangent(TrackGeometry component, TrackWaypoint waypoint)
    {
        var next = component.Next(waypoint);
        var previous = component.Previous(waypoint);

        var normal0 = next.position - waypoint.position;
        var normal1 = waypoint.position - previous.position;

        var tangent0 = Vector3.Cross(normal0, waypoint.up).normalized;
        var tangent1 = Vector3.Cross(normal1, waypoint.up).normalized;

        var tangent = Vector3.Lerp(tangent0, tangent1, 0.5f).normalized;

        waypoint.tangent = tangent;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
    static void DrawGizmoForWaypointsEditor(TrackGeometry component, GizmoType gizmoType)
    {
        Profiler.BeginSample("Draw Waypoints Editor");

        Gizmos.color = Color.green;
        foreach (var waypoint in component.waypoints)
        {
            var size = 1f;
            if(waypoint.index == 0)
            {
                size = 2f;
            }
            Gizmos.DrawWireSphere(waypoint.position, size);
        }

        Gizmos.color = Color.yellow;
        foreach (var waypoint in component.highlighted)
        {
            Gizmos.DrawWireSphere(waypoint.position, 1f);
        }

        Gizmos.color = Color.red;
        foreach (var waypoint in component.selected)
        {
            Gizmos.DrawWireSphere(waypoint.position, 1f);
        }

        Gizmos.color = Color.green;
        for (int i = 0; i < component.waypoints.Count; i++)
        {
            var waypoint = component.waypoints[i];
            var next = component.Next(waypoint);

            Gizmos.DrawLine(waypoint.position, next.position);
            Gizmos.DrawLine(waypoint.left, waypoint.right);

            Gizmos.DrawLine(waypoint.left, next.left);
            Gizmos.DrawLine(waypoint.right, next.right);
        }

        if (createWaypoints)
        {
            Gizmos.color = Color.yellow;
            if (component.highlightedPoint != null && component.lastSelected != null)
            {
                Gizmos.DrawLine(component.lastSelected.position, component.highlightedPoint.Value);
                Gizmos.DrawLine(component.lastSelected.position, component.Next(component.lastSelected).position);
            }
        }

        Profiler.EndSample();

        if (component.broadphase1d == null)
        {
            component.InitialiseBroadphase();
        }
        //component.broadphase.OnDrawGizmos();
    }
}
