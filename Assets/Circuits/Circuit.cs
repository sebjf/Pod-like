using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CircuitInfoExtensions
{
    public static Vector3 ToUnity(this AssetsInfo.Vector3 v)
    {
        Vector3 result = new Vector3();
        result.x = -v.x;
        result.y = v.y;
        result.z = v.z;
        return result;
    }
}


public class Circuit : MonoBehaviour {

    public TextAsset circuitInfo;

    public Vector3[] GridPositions;
    public Vector3 StartDirection;

    public void Reset()
    {
        StartDirection = Vector3.forward;
    }

    public void ImportCircuitInfo()
    {
        var circuit = AssetsInfo.CircuitInfo.Load(circuitInfo.bytes);

        // process grid positions

        GridPositions = new Vector3[8];

        var pole = circuit.grid.pole.ToUnity();
        var second = circuit.grid.second.ToUnity();
        var third = circuit.grid.third.ToUnity();
        var forward = circuit.grid.forward.ToUnity();

        var deltax = second - pole;
        var deltay = third - pole;

        var gridPositions = new List<Vector3>();

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 2; x++)
            {
                var position = pole + (deltay * y) + (deltax * x);
                gridPositions.Add(position);
            }
        }

        StartDirection = forward;

        GridPositions = gridPositions.ToArray();
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
