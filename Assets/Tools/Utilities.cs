using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Util
{
    //https://stackoverflow.com/questions/1082917/
    public static int repeat(int k, int n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    public static float repeat(float k, float n)
    {
        return ((k %= n) < 0) ? k + n : k;
    }

    public static void SetLayer(GameObject obj, string layer)
    {
        foreach (var trans in obj.GetComponentsInChildren<Transform>(true))
        {
            trans.gameObject.layer = LayerMask.NameToLayer(layer);
        }
    }
}
