using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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

    public static void Swap<T>(this IList<T> list, int a, int b)
    {
        T temp = list[a];
        list[a] = list[b];
        list[b] = temp;
    }

    public class Trigger<T> where T : IComparable
    {
        public UnityEvent OnChanged;
        public T value;
        private bool changed;

        public bool Changed
        {
            get
            {
                if (changed)
                {
                    changed = false;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public Trigger()
        {
            OnChanged = new UnityEvent();
            changed = false;
        }

        public void Update(T number)
        {
            if (number.CompareTo(value) != 0)
            {
                value = number;
                OnChanged.Invoke();
                changed = true;
            }
        }
    }
}
