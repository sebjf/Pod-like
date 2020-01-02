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

    public float brakingTorque;

    public bool steers = false;
    public bool drives = true;

    [HideInInspector]
    public float steerAngle = 0f;

    private float displacement;
    private float prevDisplacement;
    private float displacementVel;

    private Vector3 position;
    private Vector3 prevPosition;
    public Vector3 velocity { get; private set; }
    private Vector3 forward;
    private Quaternion rotation;

    public Vector3 up { get; private set; }
    public Vector3 right { get; private set; }
    public bool inContact { get; private set; }

    public float angularVelocity { get; private set; }
    public float angle { get; private set; }

    private Vector3 road;
    private float coefficientOfFriction = 1f;

    private float slipForceRange;

    [HideInInspector]
    public float wheelsInContact;

    public float Vt
    {
        get
        {
            return Vector3.Dot(velocity, right);
        }
    }

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

    //private GraphOverlay.Annotation annotation;
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

        slipForce.postWrapMode = WrapMode.ClampForever;

        //annotation = FindObjectOfType<GraphOverlay>().CreateAnnotation();
        //annotation.world = transform;
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
        var angularAcceleration = -(brakingTorque * power) / inertia;
        angularVelocity -= Mathf.Min(Mathf.Abs(angularVelocity), Mathf.Abs(angularAcceleration * Time.fixedDeltaTime)) * Mathf.Sign(angularVelocity);
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
            road = m_raycastHit.normal;
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
        var Fsuspension = (k * displacement + B * displacementVel) * up;

        rigidBody.AddForceAtPosition(Fsuspension, attachmentPoint);
    }

    public void UpdateDriveForce()
    {
        var Vr = Vector3.Dot(velocity, forward);

        //Jung, S., Kim, T.Y., &Yoo, W.S. (2018). Advanced slip ratio for ensuring numerical stability of low - speed driving simulation. Part I: Longitudinal slip ratio.
        //Proceedings of the Institution of Mechanical Engineers, Part D: Journal of Automobile Engineering. http://doi.org/10.1177/0954407018759738
        var Vtm = 1.1f * (Time.fixedDeltaTime / 2) * forwardSlipScale * (rigidBody.mass / wheelsInContact) * (((radius * radius) / inertia) + ( 1f / (rigidBody.mass / wheelsInContact)));

        var slip = ((angularVelocity * radius) - Vr) / (Mathf.Max(Mathf.Abs(Vr), Vtm));

        if(float.IsNaN(slip))
        {
            slip = 0f;
        }

        var Fr = forwardSlipForce.Evaluate(Mathf.Abs(slip)) * Mathf.Sign(slip) * forwardSlipScale * (rigidBody.mass / wheelsInContact);

        rigidBody.AddForceAtPosition(forward * Fr, attachmentPoint);

        var tractionTorque = -(Fr * radius);
        var tractionAngularAcceleration = tractionTorque / inertia;

        var rollAngularVelocity = Vr / radius;
        var angularDeltaV1 = rollAngularVelocity - angularVelocity;
        var angularDeltaV2 = tractionAngularAcceleration * Time.fixedDeltaTime;
        var angularDeltaVDirection = Mathf.Sign(angularDeltaV1);
        var angularDeltaV = Mathf.Min(Mathf.Abs(angularDeltaV1), Mathf.Abs(angularDeltaV2)) * angularDeltaVDirection;

        //Debug.Log(Vtm.ToString() + " " + ((angularVelocity * radius)).ToString() + " " + Vr + " " + slip.ToString() + " " + Fr.ToString());

        angularVelocity += angularDeltaV;


        var g_acceleration = Vector3.Dot(Physics.gravity, forward) * forward;
        var g_force = g_acceleration * (rigidBody.mass / wheelsInContact);
        var Fn = Vector3.Dot(Physics.gravity * (rigidBody.mass / wheelsInContact), -road) * coefficientOfFriction;
        var g_force_mag = Mathf.Min(g_force.magnitude, Fn);
        rigidBody.AddForce(-g_force.normalized * g_force_mag, ForceMode.Force);
    }

    public void UpdateGripForce()
    {
        var r = (attachmentPoint - rigidBody.worldCenterOfMass).magnitude;
        var Vt = Vector3.Dot(velocity, right) * right;
        var at = Vt / Time.fixedDeltaTime;

        // The force necessary to stop the current attachment point's lateral movement based on its current velocity

        var a = (-at / r) / wheelsInContact;
        Quaternion q = rotation * rigidBody.inertiaTensorRotation;
        a = q * Vector3.Scale(rigidBody.inertiaTensor, (Quaternion.Inverse(q) * a));
        var Ft = a / r;

        // The lateral force determined by the slip angle from the direction of travel

        var slipAngle = Mathf.Abs(Mathf.Acos(Mathf.Clamp(Vector3.Dot(velocity.normalized, forward),-1f,1f)));
        var Fs = slipForce.Evaluate(slipAngle * Mathf.Rad2Deg) * slipForceScale * (rigidBody.mass / wheelsInContact);

        Ft = Ft.normalized * Mathf.Min(Ft.magnitude, Fs);
        rigidBody.AddForceAtPosition(Ft, attachmentPoint, ForceMode.Force);

        //Debug.DrawLine(attachmentPoint, attachmentPoint + Ft, Color.red);

        var g_acceleration = Vector3.Dot(Physics.gravity, right) * right;
        var g_force = g_acceleration * (rigidBody.mass / wheelsInContact);
        var Fn = Vector3.Dot(Physics.gravity * (rigidBody.mass / wheelsInContact), -road) * coefficientOfFriction;
        var g_force_mag = Mathf.Min(g_force.magnitude, Fn);
        rigidBody.AddForce(-g_force.normalized * g_force_mag, ForceMode.Force);


        //Debug.Log(Fn.ToString() + " " + g_force.magnitude.ToString());
    }
}
