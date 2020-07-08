using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Assets/Car")]
public class Car : ScriptableObject
{
    public string Name;
    public GameObject Player;
    public GameObject Agent;
}
