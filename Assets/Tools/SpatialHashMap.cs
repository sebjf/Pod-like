using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Teschner, M., Heidelberger, B., Müller, M., Pomeranets, D., & Gross, M. (2003). Optimized Spatial //Hashing for Collision Detection of Deformable Objects. In Proceedings of Vision, Modeling, //Visualization VMV’03 (pp. 47--54). http://doi.org/10.1.1.4.5881
class SpatialHashMap
{
    public SpatialHashMap(float cellsize)
    {
        d = cellsize;
    }

    private float d;

    public int Hash(Vector3 v)
    {
        return ((Mathf.FloorToInt(v.x / d) * 73856093) ^
                (Mathf.FloorToInt(v.y / d) * 19349663) ^
                (Mathf.FloorToInt(v.z / d) * 83492791));
    }

    public Dictionary<int, List<Set>> HashTable = new Dictionary<int, List<Set>>();

    public class Set
    {
        public Vector3 position;
        public List<int> indices = new List<int>();

        public Set(Vector3 position, int i)
        {
            this.position = position;
            indices.Add(i);
        }
    }

    public void Add(Vector3 v, int i)
    {
        int h = Hash(v);
        if (!HashTable.ContainsKey(h))
        {
            HashTable.Add(h, new List<Set>());
        }
        foreach (var set in HashTable[h])
        {
            if((set.position - v).magnitude < Mathf.Epsilon)
            {
                set.indices.Add(i);
                return;
            }
        }
        HashTable[h].Add(new Set(v,i));
    }

    public IEnumerable<Set> Sets
    {
        get
        {
            foreach (var item in HashTable.Values)
            {
                foreach (var set in item)
                {
                    yield return set;
                }
            }
        }
    }
}