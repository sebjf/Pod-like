using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class GPUBuffer
{
    public ComputeBuffer Buffer { get; private set; }

    public GPUBuffer(ComputeBuffer buffer)
    {
        Buffer = buffer;
    }

    public virtual int Length
    {
        get
        {
            return Buffer.count;
        }
    }

    public virtual void Release()
    {
        if (Buffer != null)
        {
            Buffer.Release();
            Buffer = null;
        }
    }
}

public class GPUBuffer<T> : GPUBuffer
{
    public GPUBuffer(ComputeBuffer buffer) : base(buffer)
    {
        data = new T[buffer.count];
    }

    public GPUBuffer(int length) : this(new ComputeBuffer(length, Marshal.SizeOf(typeof(T))))
    {
    }

    public GPUBuffer(int length, ComputeBufferType type) : this(new ComputeBuffer(length, Marshal.SizeOf(typeof(T)), type))
    {
    }

    public GPUBuffer(Array Data) : this(new ComputeBuffer(Data.Length, Marshal.SizeOf(typeof(T))))
    {
        Buffer.SetData(Data);
    }

    protected T[] data;

    public virtual void Get()
    {
        Buffer.GetData(data);
    }

    public virtual void Set()
    {
        Buffer.SetData(data);
    }

    public virtual T[] Data
    {
        get
        {
            Buffer.GetData(data);
            return data;
        }
    }
}