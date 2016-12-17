#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CameraControls))]
public class CameraControlsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        CameraControls cameraControls = (CameraControls)target;

        if (GUILayout.Button("Update"))
        {
            cameraControls.CameraLookAtWorldCenter();
        }
    }

}
#endif