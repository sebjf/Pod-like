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

    public Dictionary<int, List<int>> HashTable = new Dictionary<int, List<int>>();

    public void Add(Vector3 v, int i)
    {
        int h = Hash(v);
        if (!HashTable.ContainsKey(h))
        {
            HashTable.Add(h, new List<int>());
        }
        HashTable[h].Add(i);
    }
}