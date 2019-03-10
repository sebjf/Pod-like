using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Wheel))]
[CanEditMultipleObjects]
public class WheelInspector : Editor
{
    private void OnSceneGUI()
    {
        var wheel = target as Wheel;
    }
}


public class WheelGizmoDrawer
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.InSelectionHierarchy)]
    static void DrawGizmoForWheel(Wheel wheel, GizmoType gizmoType)
    {
        wheel.UpdateTransforms();
     //   wheel.SetHeight( wheel.gameObject.GetComponentInParent<Rigidbody>().mass );

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(wheel.attachmentPoint, 0.1f);

        var offsetEnd = wheel.attachmentPoint - wheel.up * wheel.offset;

        Gizmos.DrawLine(wheel.attachmentPoint, offsetEnd);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(offsetEnd, offsetEnd - wheel.up * wheel.travel);

        var pose = wheel.attachmentPoint + -wheel.up * wheel.height;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pose, 0.075f);

        // yes we can use handles in gizmos
        Handles.color = Color.green;
        Handles.DrawWireDisc(pose, wheel.right, wheel.radius);

        var designtimepose = wheel.attachmentPoint + -wheel.up * ( wheel.localAttachmentPosition.y - wheel.transform.localPosition.y);
        Gizmos.DrawWireSphere(designtimepose, 0.05f);
    }
}
