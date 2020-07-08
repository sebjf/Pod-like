using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

// This single query approach won't make much difference when driving one or two cars, 
// but when collecting experience for 100's (with concomitant cache invalidations) it will.
public struct PathQuery
{
    public Vector3 Midpoint;
    public Vector3 Tangent;
    public Vector3 Forward;
    public float Curvature;
    public float Inclination;
    public float Camber;

    public Vector3 Up
    {
        get
        {
            return Vector3.Cross(Tangent, Forward).normalized;
        }
    }
}

public struct TrackSection
{
    public Vector3 left;
    public Vector3 right;
    public float trackdistance;
    public bool jump;
}

public struct TrackFlags
{
    public bool nospawn;
    public bool jumprules;
}

public abstract class TrackPath : MonoBehaviour
{
    public float totalLength;

    public abstract float Distance(Vector3 position, float lastDistance);

    public abstract PathQuery Query(float distance);
    public abstract TrackFlags Flags(float distance);
    public abstract float TrackDistance(float distance);
    public abstract TrackSection TrackSection(float distance);

    /// <summary>
    /// Computes the curvature of Y. (Where X is *ahead* by h and Z is behind by h.)
    /// </summary>
    public static float Curvature(Vector3 X, Vector3 Y, Vector3 Z)
    {
        // Project onto ground, so we don't include curvature due to inclination, which is a separate property.
        X.y = 0;
        Y.y = 0;
        Z.y = 0;

        var YX = X - Y;
        var YZ = Z - Y;
        var ZY = Y - Z;

        // Compute the direction of the curve
        var direction = Mathf.Sign(Vector3.Dot(Vector3.Cross(ZY.normalized, YX.normalized), Vector3.up));

        // https://en.wikipedia.org/wiki/Menger_curvature
        var curvature = (2f * Mathf.Sin(Mathf.Acos(Vector3.Dot(YX.normalized, YZ.normalized)))) / (X - Z).magnitude;

        curvature *= direction;

        if (float.IsNaN(curvature))
        {
            curvature = 0f;
        }

        return curvature;
    }

    public static float Inclination(Vector3 forward)
    {
        return Vector3.Dot(forward, Vector3.up); // grade is the ratio of height to distance. dot will return the proportion of forward to up/height.
    }

    public virtual string UniqueName()
    {
        return name + " " + GetType().Name;
    }
}