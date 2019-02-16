using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    public TextAsset carInfo;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Import()
    {
        var car = AssetsInfo.CarInfo.Load(carInfo.bytes);

        transform.localPosition = car.Chassis.ToUnity();

        var halfWheelHeight = transform.Find("WheelRearL").GetComponentInChildren<MeshFilter>().sharedMesh.bounds.extents.y;

        transform.Find("WheelRearL").localPosition = new Vector3(car.WheelRearL.x, -car.Chassis.y + halfWheelHeight, car.WheelRearL.z);
        transform.Find("WheelFrontL").localPosition = new Vector3(car.WheelFrontL.x, -car.Chassis.y + halfWheelHeight, car.WheelFrontL.z);
        transform.Find("WheelRearR").localPosition = new Vector3(car.WheelRearR.x, -car.Chassis.y + halfWheelHeight, car.WheelRearR.z);
        transform.Find("WheelFrontR").localPosition = new Vector3(car.WheelFrontR.x, -car.Chassis.y + halfWheelHeight, car.WheelFrontR.z);
    }
}
