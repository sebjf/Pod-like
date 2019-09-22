using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CameraPanelController : MonoBehaviour
{
    DriftCamera cameraController;

    public GameObject ButtonPrototype;

    List<Button> buttons;

    private void Awake()
    {
        cameraController = GetComponentInParent<DriftCamera>();
        buttons = GetComponentsInChildren<Button>().ToList();
    }

    void Start()
    {
        while(buttons.Count < cameraController.cameraRigs.Length)
        {
            buttons.Add(
                GameObject.Instantiate(ButtonPrototype, ButtonPrototype.transform.parent)
                .GetComponent<Button>()
                );
        }

        for (int i = 0; i < cameraController.cameraRigs.Length; i++)
        {
            buttons[i].GetComponent<CameraButton>().cameraController = cameraController;
            buttons[i].GetComponent<CameraButton>().cameraRig = cameraController.cameraRigs[i];
            buttons[i].GetComponentInChildren<Text>().text = cameraController.cameraRigs[i].gameObject.name;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
