using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainingAgentsManager : MonoBehaviour
{
    public int numCars;
    public GameObject carPrefab;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void PlaceCars()
    {
        foreach(Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        var waypoints = GetComponentInParent<Waypoints>();
        float spacing = waypoints.totalLength / numCars;

        for (int i = 0; i < numCars; i++)
        {
            var car = GameObject.Instantiate(carPrefab);
            PrepCar(car);
            car.transform.SetParent(this.transform);
            car.transform.position = waypoints.Midline(spacing * i) + Vector3.up * 2;
            car.transform.forward = waypoints.Normal(spacing * i);
        }
    }

    private void PrepCar(GameObject car)
    {
        DestroyImmediate(car.GetComponentInChildren<DeformationModel>());
        DestroyImmediate(car.GetComponentInChildren<DynamicMesh>());
        car.GetComponent<VehicleControllerInput>().enabled = false;
        car.GetComponent<VehicleAgent>().enabled = true;
        car.SetActive(true);
    }

}
