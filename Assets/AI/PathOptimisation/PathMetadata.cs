using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathMetadata : MonoBehaviour
{
    public int Resolution;
    private TrackGeometry track;

    [Serializable]
    public class Node
    {
        public float x;
        public float i;
    }

    [HideInInspector]
    public Node[] nodes;

    public void Generate()
    {
        track = GetComponentInParent<TrackGeometry>();

        var numWaypoints = Mathf.CeilToInt(track.totalLength / Resolution);
        var trueResolution = track.totalLength / numWaypoints;

        nodes = new Node[numWaypoints];
        for (int i = 0; i < numWaypoints; i++)
        {
            nodes[i] = new Node();
            nodes[i].x = i * trueResolution;
        }

        EvaluateInclinationChange();
    }

    public void Export()
    {

    }

    public void EvaluateInclinationChange()
    {
        foreach (var node in nodes)
        {
            // look for areas of high negative inclination change,
            // and high camber in the opposite direction of curvature

            var Q = track.Query(node.x);

            var before = Q.Midpoint.y;
            var after = track.Query(node.x + Resolution).Midpoint.y;
            var b = (before - after) * 0.6f;
            node.i += Mathf.Clamp(b, 0, float.PositiveInfinity);

            var a = Q.Curvature * 10f * -Q.Camber * 200f;
            node.i += Mathf.Clamp(a, 0, float.PositiveInfinity);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if (!enabled)
        {
            return;
        }
        if (nodes != null)
        {
            if (track == null)
            {
                track = GetComponentInParent<TrackGeometry>();
            }

            foreach (var item in nodes)
            {
                var wp = track.Query(item.x).Midpoint;
                Gizmos.color = new Color(item.i, item.i, item.i);
                Gizmos.DrawWireSphere(wp, 0.5f);
            }
        }
    }
}
