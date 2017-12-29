using AWM.Controls;
using AWM.System;
using UnityEngine;

namespace AWM.UI
{
    [ExecuteInEditMode]
    public class RotateWorldSpaceUIToCamera : MonoBehaviour
    {
        private Camera m_cameraToOrientTo;

        void Start()
        {
            if (Application.isPlaying)
            {
                CameraControls cameraControls;

                if (CC.MBR.TryGet(out cameraControls))
                {
                    m_cameraToOrientTo = cameraControls.SecondaryCameraToControl;
                }
            }
            else
            {
                m_cameraToOrientTo = Camera.main;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            Vector3 v = m_cameraToOrientTo.transform.position - transform.position;
            v.x = v.z = 0.0f;
            transform.LookAt(m_cameraToOrientTo.transform.position - v);
            transform.rotation = m_cameraToOrientTo.transform.rotation;
        }
    }
}
