using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Wheel : MonoBehaviour
{
    public float k;
    public float B;
    public float offset;
    public float travel;
    public float radius;

    public AnimationCurve slipForce;
    public float slipForceScale;

    public bool steers = false;

    [HideInInspector]
    public float steerAngle = 0f;

    private float rotationAngle = 0f;

    private float displacement;
    private float prevDisplacement;
    private float displacementVel;

    private Vector3 position;
    private Vector3 prevPosition;
    private Vector3 velocity;
    private Vector3 forward;
    private Quaternion rotation;

    public Vector3 up { get; private set; }
    public Vector3 right { get; private set; }
    public bool inContact { get; private set; }

    public float angularVelocity { get; private set; }

    /// <summary>
    /// The offset from the parent transform where the forces should be applied
    /// </summary>
    public Vector3 localAttachmentPosition;

    /// <summary>
    /// The attachment point in world space
    /// </summary>
    public Vector3 attachmentPoint { get; private set; }

    /// <summary>
    /// The distance of the wheel to the attachment point along the (local) y-axis. 
    /// This can be set at design time to check the appearance of the wheel. At runtime this will be overriden with the true value.
    /// </summary>
    public float height; 

    private GraphOverlay.Annotation annotation;
    private Rigidbody rigidBody;

    private void Reset()
    {
        localAttachmentPosition = transform.localPosition + new Vector3(0, 0.5f, 0);
    }

    private void Awake()
    {
        rigidBody = GetComponentInParent<Rigidbody>();
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateTransforms();

        position = attachmentPoint;
        prevPosition = attachmentPoint;

        annotation = FindObjectOfType<GraphOverlay>().CreateAnnotation();
        annotation.world = transform;
    }

    public void UpdateTransforms()
    {
        attachmentPoint = transform.parent.TransformPoint(localAttachmentPosition);

        position = attachmentPoint;

        rotation = transform.parent.rotation * Quaternion.Euler(0, steerAngle, 0);

        up = rotation * Vector3.up;
        forward = rotation * Vector3.forward;
        right = rotation * Vector3.right;
    }

    public void UpdateVelocity()
    {
        // its important to compute the wheel velocities independently because they will include a component of the rb's angular velocity linearly
        velocity = (position - prevPosition) / Time.fixedDeltaTime;
        prevPosition = position;

        rotationAngle += angularVelocity * Time.fixedDeltaTime;
    }

    public void UpdateLocalTransform()
    {
        var localpose = transform.localPosition;
        localpose.y = localAttachmentPosition.y - height;
        transform.localPosition = localpose;

        transform.localRotation = Quaternion.Euler(rotationAngle * Mathf.Rad2Deg, steerAngle, 0);
    }

    public void UpdateSuspensionForce()
    {
        prevDisplacement = displacement;

        var restDistance = offset + travel;

        RaycastHit m_raycastHit;
        bool result = Physics.Raycast(new Ray(attachmentPoint, -up), out m_raycastHit, offset + travel + radius);
        if (result)
        {
            displacement = restDistance - (m_raycastHit.distance - radius);
            inContact = true;
        }
        else
        {
            displacement = 0;
            inContact = false;
        }

        displacement = Mathf.Clamp(displacement, 0, float.MaxValue);
        height = restDistance - displacement;

        displacementVel = (displacement - prevDisplacement) / Time.deltaTime;
        var Fsuspension = -k * displacement - B * displacementVel;

        rigidBody.AddForceAtPosition(up * -Fsuspension, attachmentPoint);
    }

    public void UpdateDriveForce(float torque)
    {
        var Vr = Vector3.Dot(velocity, forward) * forward;
        angularVelocity = Vr.magnitude / radius;
 
        var Ff = torque;    // pretend radius is 1
        rigidBody.AddForceAtPosition(forward * Ff, attachmentPoint);
    }

    public void UpdateGripForce(float wheelsInContact)
    {
        var r = (attachmentPoint - rigidBody.worldCenterOfMass).magnitude;
        var Vt = Vector3.Dot(velocity, right) * right;
        var at = Vt / Time.fixedDeltaTime;

        var a = (-at / r) / wheelsInContact;
        Quaternion q = rotation * rigidBody.inertiaTensorRotation;
        a = q * Vector3.Scale(rigidBody.inertiaTensor, (Quaternion.Inverse(q) * a));
        var Ft = a / r;
        
        var slipAngle = Mathf.Abs(Vector3.Dot(velocity, forward));
        var Fs = slipForce.Evaluate(slipAngle * Mathf.Rad2Deg) * slipForceScale;

        Ft = Ft.normalized * Mathf.Min(Ft.magnitude, Fs);
        rigidBody.AddForceAtPosition(Ft, attachmentPoint, ForceMode.Force);

        if (annotation != null)
        {
            annotation.label = Ft.magnitude.ToString();
        }

        //Debug.DrawLine(attachmentPoint, attachmentPoint + Ft, Color.red);
    }
}
