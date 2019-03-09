using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Wheel : MonoBehaviour
{
    public float k;
    public float B;
    public float length;
    public float travel;

    public float lateralForce;

    public float maxTorque;

    public bool steers = false;

    private float displacement;
    private float prevDisplacement;
    private float displacementVel;

    private RaycastHit m_raycastHit;

    private Vector3 position;
    private Vector3 prevPosition;
    public Vector3 velocity;

    private GraphOverlay.Annotation annotation;

    // Start is called before the first frame update
    void Start()
    {
        position = transform.position;
        prevPosition = transform.position;

        annotation = FindObjectOfType<GraphOverlay>().CreateAnnotation();
        annotation.world = transform;
    }

    private void Update()
    {
        if (steers)
        {
            float angle = 45 * Input.GetAxis("Horizontal");
            transform.localRotation = Quaternion.Euler(0, angle, 0);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        var rigidBody = GetComponentInParent<Rigidbody>();

        // Suspension

        prevDisplacement = displacement;

        bool result = Physics.Raycast(new Ray(transform.position, -transform.up), out m_raycastHit, travel);
        if(result)
        {
            var intersection = (m_raycastHit.point - transform.position).magnitude;
            displacement = length - intersection;
        }
        else
        {
            displacement = 0;
        }

        displacementVel = (displacement - prevDisplacement) / Time.deltaTime;

        var Fsuspension = -k * displacement - B * displacementVel;

        rigidBody.AddForceAtPosition(transform.up * -Fsuspension, transform.position);

        // Drive

        float torque = maxTorque * Input.GetAxis("Vertical");

        // pretend radius is 1

        var Ff = torque;

        rigidBody.AddForceAtPosition(transform.forward * Ff, transform.position);


        // Grip

        // its important to compute the wheel velocities independently because they will include a component of the rb's angular velocity linearly
        position = transform.position;
        velocity = (position - prevPosition) / Time.fixedDeltaTime;
        prevPosition = position;

        var r = (transform.position - rigidBody.worldCenterOfMass).magnitude;
        var Vt = Vector3.Dot(velocity, transform.right) * transform.right;
        var at = Vt / Time.fixedDeltaTime;

        var a = (-at / r) / 4;
        Quaternion q = transform.rotation * rigidBody.inertiaTensorRotation;
        a = q * Vector3.Scale(rigidBody.inertiaTensor, (Quaternion.Inverse(q) * a));
        var Ft = a / r;

        var Fs = lateralForce;
        var slipAngle = Vector3.Dot(velocity, transform.forward);
        if(slipAngle < Mathf.Sin(Mathf.Deg2Rad * 10))
        {
            Fs = 0;
        }

        Ft = Ft.normalized * Mathf.Min(Ft.magnitude, Fs);
        rigidBody.AddForceAtPosition(Ft, transform.position, ForceMode.Force);

        if (annotation != null)
        {
            annotation.label = Ft.magnitude.ToString();
        }

        Debug.DrawLine(transform.position, transform.position + Ft, Color.red);
    }

}
