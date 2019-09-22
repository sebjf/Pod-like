using System;
using System.Linq;
using UnityEngine;

public class DriftCamera : MonoBehaviour
{
    [Serializable]
    public class AdvancedOptions
    {
        public bool updateCameraInUpdate;
        public bool updateCameraInFixedUpdate = true;
        public bool updateCameraInLateUpdate;
        public KeyCode switchViewKey = KeyCode.Space;
    }

    [HideInInspector]
    public CamRig[] cameraRigs;

    public CamRig cameraRig;

    public float smoothing = 6f;
    public AdvancedOptions advancedOptions;

    bool m_ShowingSideView;

    private void Start()
    {
        cameraRigs = FindObjectsOfType<CamRig>().Where(x => x.enabled).ToArray();
        if (cameraRig == null)
        {
            cameraRig = cameraRigs[0];
        }
    }
    private void FixedUpdate ()
    {
        if(advancedOptions.updateCameraInFixedUpdate)
            UpdateCamera ();
    }

    private void Update ()
    {
        if (Input.GetKeyDown (advancedOptions.switchViewKey))
            m_ShowingSideView = !m_ShowingSideView;

        if(advancedOptions.updateCameraInUpdate)
            UpdateCamera ();
    }

    private void LateUpdate ()
    {
        if(advancedOptions.updateCameraInLateUpdate)
            UpdateCamera ();
    }

    private void UpdateCamera ()
    {
        if (m_ShowingSideView)
        {
            transform.position = cameraRig.sideView.position;
            transform.rotation = cameraRig.sideView.transform.rotation;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, cameraRig.positionTarget.position, Time.deltaTime * smoothing);
            transform.LookAt(cameraRig.lookAtTarget);
        }
    }
}
