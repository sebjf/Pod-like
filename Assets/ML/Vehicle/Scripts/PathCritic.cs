using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathCritic : MonoBehaviour
{
    private Navigator navigator;
    private TrackGeometry waypoints;
    private Rigidbody body;

    public GraphOverlay graph;

    [HideInInspector]
    public float reward;

    [HideInInspector]
    public float lateralError;

    private void Awake()
    {
        navigator = GetComponent<Navigator>();
        waypoints = GetComponentInParent<TrackGeometry>();
        body = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var pathFollowingError = (waypoints.Evaluate(navigator.TrackDistance) - body.position);

        lateralError = new Vector3(pathFollowingError.x, 0, pathFollowingError.z).magnitude * 0.01f;

        var distanceTravelledReward = (navigator.distanceTravelledInFrame / Time.fixedDeltaTime) / 500f;

        reward = 0f;
        reward += -0.001f; // gentle disincentive to sit still
        reward += -lateralError * 5;
        reward += distanceTravelledReward;

        if (graph) graph.GetSeries("lateralError", Color.red).values.Add(-lateralError);
        if (graph) graph.GetSeries("reward", Color.green).values.Add(reward);
        if (graph) graph.GetSeries("distance", Color.blue).values.Add(distanceTravelledReward);
    }
}
