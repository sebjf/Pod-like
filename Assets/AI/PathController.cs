using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathController : MonoBehaviour
{
    public float samplingDistance = 10;
    public int sampingCount = 20;
    public float targetDistance = 10;
    public float speedBias = 0.95f;
    public float maxDeceleration = 1f;
    public float maxAcceleration = 1f;

    private TrackPath path;
    private Autopilot pilot;
    private Navigator navigator;
    private new Rigidbody rigidbody;

    private float[] curvatureProfile;
    private float[] profile;

    private Wheel[] wheels;

    public GraphOverlay graph;

    private void Awake()
    {
        path = GetComponentInParent<TrackGeometry>();
        pilot = GetComponent<Autopilot>();
        navigator = GetComponent<Navigator>();
        wheels = GetComponentsInChildren<Wheel>().ToArray();
        rigidbody = GetComponent<Rigidbody>();

        curvatureProfile = new float[sampingCount];
        profile = new float[sampingCount];
    }

    public float MaxGripForce()
    {
        return wheels.Average(x => x.slipForceScale) * Physics.gravity.magnitude;
    }

    public float Speed(float curvature)
    {
        // The maximum cornering speed for a given curvature (1/r). Based on 
        // the maximum lateral forces available from the current slip curves.

        curvature = Mathf.Max(curvature, 0.00001f); // avoid divide by zero

        return Mathf.Sqrt((1f / Mathf.Abs(curvature)) * MaxGripForce());
    }

    public float MaxDeceleration(float speed)
    {
        return maxDeceleration;
    }

    public float MaxAcceleration(float speed)
    {
        return maxAcceleration;
    }

    public void UpdateSpeedProfile()
    {
        // Optimum-time profile method based on Subostis & Gerde's backwards-forwards integration.
        // Subosits, J., & Gerdes, J. C. (2015).  Autonomous vehicle control for emergency maneuvers: The effect of topography. 
        // Proceedings of the American Control Conference, 2015-July, 1405–1410. 
        // https://doi.org/10.1109/ACC.2015.7170930

        // The ideal speed profile (algebraic constraint)

        for (int i = 0; i < profile.Length; i++)
        {
            profile[i] = Speed(Mathf.Abs(curvatureProfile[i])) * speedBias;
        }

        graph.GetSeries("ideal").scale = 0.001f;
        graph.GetSeries("ideal").color = Color.blue;
        graph.GetSeries("ideal").values.Clear();
        graph.GetSeries("ideal").values.AddRange(profile);

        // The updated profile based on acceleration and deceleration limits

        for (int i = profile.Length - 1; i > 0; i--)
        {
            profile[i - 1] = Mathf.Min(profile[i - 1], profile[i] + Mathf.Sqrt(2 * MaxDeceleration(profile[i])));
        }

        graph.GetSeries("backwards").scale = 0.001f;
        graph.GetSeries("backwards").color = Color.red;
        graph.GetSeries("backwards").values.Clear();
        graph.GetSeries("backwards").values.AddRange(profile);

        for (int i = 0; i < profile.Length - 1; i++)
        {
            profile[i + 1] = Mathf.Min(profile[i + 1], profile[i] + Mathf.Sqrt(2 * MaxAcceleration(profile[i])));
        }

        graph.GetSeries("forwards").scale = 0.001f;
        graph.GetSeries("forwards").color = Color.green;
        graph.GetSeries("forwards").values.Clear();
        graph.GetSeries("forwards").values.AddRange(profile);
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < curvatureProfile.Length; i++)
        {
            curvatureProfile[i] = path.Curvature(navigator.TrackDistance + i * samplingDistance);
        }

        UpdateSpeedProfile();

        pilot.speed = profile[0];
    }
}
