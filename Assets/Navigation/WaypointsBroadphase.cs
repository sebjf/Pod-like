using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;

public class WaypointsBroadphase : ScriptableObject
{
    [Serializable]
    public class BoundingSphere
    {
        public Vector3 center;
        public float radius;

        public BoundingSphere left;
        public BoundingSphere right;

        public int result;

        public bool leaf;
    }

    [SerializeField]
    private float resolution;

    [SerializeField]
    private float trackLength;

    [SerializeField]
    private BoundingSphere bst;

    [SerializeField]
    private int[] indices;

    public void Initialise(Waypoints component)
    {
        var waypoints = component.waypoints;
        trackLength = component.totalLength;

        // 1d

        resolution = 1f;
        indices = new int[Mathf.CeilToInt(trackLength / resolution)];

        for (int i = 0; i < waypoints.Count; i++)
        {
            int start = Mathf.FloorToInt(waypoints[i].start / resolution);
            int end = Mathf.CeilToInt(waypoints[i].end / resolution);

            for (int c = start; c < end; c++)
            {
                indices[c] = i;
            }
        }

        // 3d

        // create all the leaf nodes

        var level1 = new List<BoundingSphere>();

        for (int i = 0; i < waypoints.Count; i++)
        {
            var left = waypoints[i];
            var right = component.Next(left);

            var sphere = new BoundingSphere();
            sphere.result = left.index;
            sphere.center = Vector3.Lerp(left.position, right.position, 0.5f);
            sphere.radius = (left.position - right.position).magnitude * 0.5f;
            sphere.leaf = true;

            level1.Add(sphere);
        }

        do
        {
            // then the tree
            var level0 = new List<BoundingSphere>();

            for (int i = 0; i < level1.Count; i += 2)
            {
                var sphere = new BoundingSphere();
                sphere.left = level1[i + 0];
                if ((i + 1) < level1.Count)
                {
                    sphere.right = level1[i + 1];
                }
                else
                {
                    sphere.right = sphere.left;
                }
                sphere.center = Vector3.Lerp(sphere.right.center, sphere.left.center, 0.5f);
                sphere.radius = ((sphere.right.center - sphere.left.center).magnitude + sphere.left.radius + sphere.right.radius) * 0.5f;
                sphere.leaf = false;

                level0.Add(sphere);
            }

            if(level0.Count == 1)
            {
                bst = level0[0];
                break;
            }

            level1 = level0;

        } while (true);
    }

    [NonSerialized]
    private Stack<BoundingSphere> stack = new Stack<BoundingSphere>();

    [NonSerialized]
    private List<int> results = new List<int>();

    public List<int> Evaluate(Vector3 position)
    {
        stack.Push(bst);
        results.Clear();

        BoundingSphere closest;
        float closestDistance = float.MaxValue;

        do
        {
            var bs = stack.Pop();

            if(bs == null)
            {
                continue;
            }

            if (bs.leaf)
            {
                results.Add(bs.result);
            }
            else
            {
                var distance = (position - bs.center).magnitude - bs.radius;
                var contains = distance <= 0;

                if(distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = bs;
                }

                if (contains)
                {
                    stack.Push(bs.left);
                    stack.Push(bs.right);
                }
            }
        } while (stack.Count > 0);

        return results;
    }

    public List<int> Evaluate(float distance)
    {
        results.Clear();

        try
        {
            results.Add(indices[Mathf.FloorToInt(distance / resolution)]);
        }catch(IndexOutOfRangeException e)
        {
            Debug.Log(distance);
            throw e;
        }

        return results;
    }

    public void OnDrawGizmos()
    {
        spheresdrawn = 0;
        DrawBoundingSphere(bst);
    }

    private int spheresdrawn;

    private void DrawBoundingSphere(BoundingSphere start)
    {
        if (!start.leaf)
        {
            DrawBoundingSphere(start.left);
            DrawBoundingSphere(start.right);
        }

        Gizmos.DrawWireSphere(start.center, start.radius);
        spheresdrawn++;
    }
}
