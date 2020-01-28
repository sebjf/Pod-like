using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CanvasCameraSwitcher : MonoBehaviour
{
    private void Update()
    {
        GetComponent<Canvas>().targetDisplay = GetComponentInParent<Camera>().targetDisplay;
    }
}
