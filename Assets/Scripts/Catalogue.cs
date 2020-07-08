using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="Assets/Catalogue")]
public class Catalogue : ScriptableObject
{
    public List<Car> cars;
    public List<Circuit> circuits;
}
