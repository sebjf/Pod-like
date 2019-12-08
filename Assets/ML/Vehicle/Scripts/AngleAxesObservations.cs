using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class AngleAxesObservations
{
    public AngleAxesObservations()
    {
        samples = new Sample[0];
        interval = 25;
        numIntervals = 20;
        balance = 0.2f;
        targetSample = (int)(numIntervals * balance + 1);
    }

    public float interval;
    public int numIntervals;
    public float balance;
    public int targetSample;

    public struct Sample
    {
        public Vector3 midpoint;
        public float width;
        public Vector3 trajectory;
        public float angle;
        public Vector3 tangent;
        public Vector3 left;
        public Vector3 right;
    }

    public Sample[] samples;

    public void Evaluate(Navigator navigator)
    {
        var waypoints = navigator.waypoints;

        if (samples == null || samples.Length < numIntervals)
        {
            samples = new Sample[numIntervals];
        }

        Profiler.BeginSample("Track Observations");

        for (int i = 0; i < numIntervals; i++)
        {
            var location = navigator.TrackDistance - (numIntervals * balance * interval) + (i * interval);
            location = Mathf.Floor(location / interval) * interval; // round down to nearest interval step

            var query = waypoints.Query(location);

            var midpoint = query.Midpoint;
            samples[i].midpoint = midpoint;
            samples[i].width = query.Width;
            samples[i].tangent = query.Tangent;
            samples[i].left = samples[i].midpoint - samples[i].tangent * samples[i].width * 0.5f;
            samples[i].right = samples[i].midpoint + samples[i].tangent * samples[i].width * 0.5f;

            var next = waypoints.Midline(location + interval);
            var prev = waypoints.Midline(location - interval);

            var trajectory = (next - midpoint).normalized;
            samples[i].trajectory = trajectory;

            var prevTrajectory = (midpoint - prev).normalized;

            samples[i].angle = Mathf.Sign(Vector3.Dot(Vector3.Cross(prevTrajectory, trajectory), Vector3.up)) * Mathf.Acos(Mathf.Clamp(Vector3.Dot(prevTrajectory, trajectory), 0f, 1f)) * (1f / Mathf.PI);
        }

        Profiler.EndSample();
    }

    public Vector3 EvaluateTargetpoint(float w)
    {
        return samples[targetSample].midpoint + samples[targetSample].tangent * w * samples[targetSample].width * 0.5f;
    }
}