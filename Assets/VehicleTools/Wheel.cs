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

    public AnimationCurve forwardSlipForce;
    public float forwardSlipScale;

    public bool steers = false;
    public bool drives = true;

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
    public float angle { get; private set; }

    public float mass;

    private float inertia;

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

        inertia = mass * (radius * radius) / 2;

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

    public void ApplyDriveTorque(float torque)
    {
        var angularAcceleration = torque / inertia;
        angularVelocity += angularAcceleration * Time.fixedDeltaTime;
    }

    public void ApplyBrake(float power)
    {

    }

    public void UpdateVelocity()
    {
        // its important to compute the wheel velocities independently because they will include a component of the rb's angular velocity linearly
        velocity = (position - prevPosition) / Time.fixedDeltaTime;
        prevPosition = position;

        angle += angularVelocity * Time.fixedDeltaTime;
    }

    public void UpdateLocalTransform()
    {
        var localpose = transform.localPosition;
        localpose.y = localAttachmentPosition.y - height;
        transform.localPosition = localpose;

        transform.localRotation = Quaternion.Euler(angle * Mathf.Rad2Deg, steerAngle, 0);
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

    public void UpdateDriveForce()
    {
        var Vr = Vector3.Dot(velocity, forward);

        var slip = ((angularVelocity * radius) - Vr) / Mathf.Abs(Vr);
        var Fr = forwardSlipForce.Evaluate(Mathf.Abs(slip)) * Mathf.Sign(slip) * forwardSlipScale;

        rigidBody.AddForceAtPosition(forward * Fr, attachmentPoint);

        var tractionTorque = -(Fr * radius);
        var tractionAngularAcceleration = tractionTorque / inertia;

        var rollAngularVelocity = Vr / radius;
        var angularDeltaV1 = rollAngularVelocity - angularVelocity;
        var angularDeltaV2 = tractionAngularAcceleration * Time.fixedDeltaTime;
        var angularDeltaVDirection = Mathf.Sign(angularDeltaV1);
        var angularDeltaV = Mathf.Min(Mathf.Abs(angularDeltaV1), Mathf.Abs(angularDeltaV2)) * angularDeltaVDirection;

      //  Debug.Log(slip.ToString() + " " + Fr.ToString() + " " + angularVelocity);

        angularVelocity += angularDeltaV;
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
