using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrainingEntityListItem : MonoBehaviour
{
    public string item;

    public bool selected
    {
        get
        {
            return GetComponentInParent<Toggle>().isOn;
        }
    }

}
