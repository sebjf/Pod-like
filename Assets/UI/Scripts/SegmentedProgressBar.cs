using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
//[ExecuteInEditMode]
public class SegmentedProgressBar : MonoBehaviour
{
    private Image image;
    private Sprite sprite;

    public float Value;

    private void Awake()
    {
        image = GetComponent<Image>();
        sprite = image.sprite;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //image.pixelsPerUnitMultiplier = 

        //image.rectTransform.right = ((1-Value) * image.rectTransform.parent.width)
    }
}
