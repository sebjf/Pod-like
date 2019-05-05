using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffersHelper
{
    private Dictionary<string, GPUBuffer> buffers = new Dictionary<string, GPUBuffer>();

    public ComputeBuffer Add(ComputeBuffer buffer, Array data, params string[] aliases)
    {
        Add(buffer, aliases);
        buffer.SetData(data);
        return buffer;
    }

    public ComputeBuffer Add(ComputeBuffer buffer, params string[] aliases)
    {
        foreach (var name in aliases)
        {
            buffers[name] = new GPUBuffer(buffer);
        }
        return buffer;
    }

    public void Add(GPUBuffer buffer, params string[] aliases)
    {
        foreach (var name in aliases)
        {
            buffers[name] = buffer;
        }
    }

    public void SetBuffers(ComputeShader shader, int kernel, params string[] names)
    {
        foreach (var name in names)
        {
            shader.SetBuffer(kernel, name, buffers[name].Buffer);
        }
    }

    public void SetBuffers(ComputeShader shader, int kernel)
    {
        SetBuffers(shader, kernel, buffers.Keys.ToArray());
    }

    public void SetBuffers(ComputeShader shader, params int[] kernels)
    {
        foreach (var k in kernels)
        {
            SetBuffers(shader, k);
        }
    }

    public void Release()
    {
        foreach (var helper in buffers.Values)
        {
            if (helper != null && helper.Buffer != null)
            {
                helper.Buffer.Release();
            }
        }
    }
}