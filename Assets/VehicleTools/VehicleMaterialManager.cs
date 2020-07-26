using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class VehicleMaterialManager : MonoBehaviour
{
    private Vehicle vehicle;
    private new MeshRenderer renderer;
    private Material[] materials;

    private void Awake()
    {
        vehicle = GetComponentInParent<Vehicle>();
        renderer = GetComponent<MeshRenderer>();
        materials = renderer.sharedMaterials;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in materials)
        {
            item.SetFloat("_Brake", vehicle.brake > 0 ? 0.8f : 0f);
        }
    }
}
