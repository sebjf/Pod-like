using System;
using System.Linq;
using UnityEngine;

public class RaceCamera : MonoBehaviour
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

    public CamRig Target;

    public float smoothing = 6f;
    public AdvancedOptions advancedOptions;

    bool m_ShowingSideView;

    private void Start()
    {
        cameraRigs =
            FindObjectsOfType<CamRig>().
            Where(x => x.enabled).ToList().
            OrderBy(c => c.priority).ToArray(); // https://stackoverflow.com/questions/15486/
        Target = cameraRigs.FirstOrDefault();
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
        if (!Target)
        {
            return;
        }

        if (m_ShowingSideView)
        {
            transform.position = Vector3.Lerp(transform.position, Target.sideView.position, Time.deltaTime * smoothing * 0.5f);
            transform.LookAt(Target.lookAtTarget);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, Target.positionTarget.position, Time.deltaTime * smoothing);
            transform.LookAt(Target.lookAtTarget);
        }
    }
}
