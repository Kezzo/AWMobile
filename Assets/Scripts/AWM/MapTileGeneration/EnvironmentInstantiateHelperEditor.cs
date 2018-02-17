#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AWM.MapTileGeneration
{
    /// <summary>
    /// Editor script to be able to generate environment props in the editor.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(EnvironmentInstantiateHelper))]
    public class EnvironmentInstantiateHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EnvironmentInstantiateHelper environmentInstantiateHelper = (EnvironmentInstantiateHelper) target;

            if (GUILayout.Button("Instantiate"))
            {
                environmentInstantiateHelper.InstantiateEnvironment();
            }

            base.OnInspectorGUI();
        }
    }
}
#endif