using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraButton : MonoBehaviour
{
    public CamRig cameraRig;
    public DriftCamera cameraController;

    private void Awake()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
        {
            cameraController.Target = cameraRig;
        }));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
