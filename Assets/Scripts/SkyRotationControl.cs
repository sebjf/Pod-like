using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyRotationControl : MonoBehaviour
{
    public Material material;

    public float rotation;
    public float rate;
    public float repeat;

    public void Update()
    {
        rotation += (rate * Time.deltaTime);
        rotation = Mathf.Repeat(rotation, 360);

        material.SetFloat("_Rotation", rotation);
        material.SetFloat("_Repeat", repeat);
    }
}
