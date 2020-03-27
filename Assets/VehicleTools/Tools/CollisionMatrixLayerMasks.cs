using UnityEngine;
using System.Collections.Generic;

// https://forum.unity.com/threads/260744/
public static class CollisionMatrixLayerMasks
{
    private static Dictionary<int, int> masksByLayer;

    private static void Init()
    {
        masksByLayer = new Dictionary<int, int>();
        for (int i = 0; i < 32; i++)
        {
            int mask = 0;
            for (int j = 0; j < 32; j++)
            {
                if (!Physics.GetIgnoreLayerCollision(i, j))
                {
                    mask |= 1 << j;
                }
            }
            masksByLayer.Add(i, mask);
        }
    }

    public static int ForLayer(int layer)
    {
        if(masksByLayer == null)
        {
            Init();
        }
        return masksByLayer[layer];
    }
}
