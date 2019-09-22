using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainingAgentsManager : MonoBehaviour
{
    public int numCars;
    public GameObject carPrefab;

    public float lateralNoise;
    public float positionNoise;
    public float orientationNoise;

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
        var children = transform.Cast<Transform>().ToList();
        foreach (Transform child in children)
        {
         //   DestroyImmediate(child.gameObject);
        }

        for (int i = 0; i < numCars - children.Count; i++)
        {
            var car = GameObject.Instantiate(carPrefab);
            PrepCar(car);
            car.transform.SetParent(this.transform);
        }

        children = transform.Cast<Transform>().ToList();

        var waypoints = GetComponentInParent<TrackGeometry>();
        float spacing = waypoints.totalLength / children.Count;

        for (int i = 0; i < children.Count; i++)
        {
            children[i].GetComponent<VehicleAgent>().ResetPositionOnTrack(spacing * i, 0, 4);
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
