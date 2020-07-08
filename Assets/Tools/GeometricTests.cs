using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GeometricTests  {

    public struct ClosestPointSegmentSegmentResult
    {
        public Vector3 c1;
        public Vector3 c2;
    }

    public struct Segment
    {
        public Vector3 p;
        public Vector3 q;

        public Segment(Vector3 p, Vector3 q)
        {
            this.p = p;
            this.q = q;
        }
    }

    public static ClosestPointSegmentSegmentResult ClosestPointSegmentSegment(Segment line1, Segment line2)
    {
        var d1 = line1.q - line1.p;
        var d2 = line2.q - line2.p;
        var r = line1.p - line2.p;
        var a = Vector3.Dot(d1, d1);
        var e = Vector3.Dot(d2, d2);
        var b = Vector3.Dot(d1, d2);
        var f = Vector3.Dot(d2, r);

        if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
        {
            return new ClosestPointSegmentSegmentResult() { c1 = line1.p, c2 = line2.p };
        }

        var s = 0f;
        var t = 0f;

        if (a <= Mathf.Epsilon)
        {
            s = 0f;
            t = f / e;
            t = Mathf.Clamp(t, 0f, 1f);
        }
        else
        {
            var c = Vector3.Dot(d1, r);
            if (e <= Mathf.Epsilon)
            {
                t = 0f;
                s = Mathf.Clamp(-c / a, 0f, 1f);
            }
            else
            {
                var denom = a * e - b * b;
                if (denom != 0f)
                {
                    s = Mathf.Clamp((b * f - c * e) / denom, 0f, 1f);
                }
                else
                {
                    s = 0f;
                }
            }

            t = (b * s + f) / e;

            if (t < 0f)
            {
                t = 0f;
                s = Mathf.Clamp(-c / a, 0f, 1f);
            }
            else if (t > 1f)
            {
                t = 1f;
                s = Mathf.Clamp((b - c) / a, 0f, 1f);
            }
        }

        var result = new ClosestPointSegmentSegmentResult();
        result.c1 = line1.p + d1 * s;
        result.c2 = line2.p + d2 * t;

        return result;
    }

    /// <summary>
    /// David Eberly, Geometric Tools, Redmond WA 98052
    /// Copyright (c) 1998-2018
    /// Distributed under the Boost Software License, Version 1.0.
    /// http://www.boost.org/LICENSE_1_0.txt
    /// http://www.geometrictools.com/License/Boost/LICENSE_1_0.txt
    /// File Version: 3.0.0 (2016/06/19)
    /// </summary>
    /// <param name="line"></param>
    /// <param name="ray"></param>
    /// <returns></returns>
    public static ClosestPointSegmentSegmentResult ClosestPointSegmentRay(Segment line, Ray ray)
    {
        ClosestPointSegmentSegmentResult result;

        Vector3 diff = line.p - ray.origin;
        var d = (line.q - line.p).normalized;
        var a01 = -Vector3.Dot(d, ray.direction);
        var b0 = Vector3.Dot(diff, d);
        var s0 = 0f;
        var s1 = 0f;

        if (Mathf.Abs(a01) < 1f)
        {
            var b1 = -Vector3.Dot(diff, ray.direction);
            s1 = a01 * b0 - b1;

            if (s1 >= 0f)
            {
                // Two interior points are closest, one on line and one on ray.
                var det = 1f - a01 * a01;
                s0 = (a01 * b1 - b0) / det;
                s1 /= det;
            }
            else
            {
                // Origin of ray and interior point of line are closest.
                s0 = -b0;
                s1 = 0f;
            }
        }
        else
        {
            // Lines are parallel, closest pair with one point at ray origin.
            s0 = -b0;
            s1 = 0f;
        }

        s0 = Mathf.Clamp(s0, 0f, (line.q - line.p).magnitude);

        result.c1 = line.p + s0 * d;
        result.c2 = ray.origin + s1 * ray.direction;

        return result;
    }

    public static Vector3 ClosestPointOnLine(Vector3 point, Segment line)
    {
        var dir = (line.q - line.p).normalized;
        var d = Vector3.Dot((point - line.p), dir);
        return line.p + (dir * d);
    }
}

