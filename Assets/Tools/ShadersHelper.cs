using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadersHelper
{

    public static int GetNextMultiple(int n, int multiple)
    {
        return (n % multiple > 0) ? (n + multiple - (n % multiple)) : n;
    }

    public static int GetThreadGroups(int threads, int groupsize)
    {
        return GetNextMultiple(threads, groupsize) / groupsize;
    }

}

class ShaderWrapper
{
    public ShaderWrapper(string resourcesshadername) : this(Resources.Load<ComputeShader>(resourcesshadername))
    {
    }

    public ShaderWrapper(ComputeShader shader)
    {
        Shader = Object.Instantiate(shader); // until unity fix their stuff https://forum.unity.com/threads/multiple-instances-of-same-compute-shader-is-it-possible.506961/
    }

    public void Dispatch(int lookup, int xthreads, int ythreads, int zthreads)
    {
        Shader.Dispatch(lookup,
            ShadersHelper.GetNextMultiple(xthreads, 64),
            ShadersHelper.GetNextMultiple(ythreads, 1),
            ShadersHelper.GetNextMultiple(zthreads, 1));
    }

    public ComputeShader Shader { get; private set; }
}