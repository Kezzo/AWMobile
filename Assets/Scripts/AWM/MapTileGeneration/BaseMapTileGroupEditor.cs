#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AWM.MapTileGeneration
{
    /// <summary>
    /// Editor script to enable function of the BaseMapTileGroup class in the editor.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BaseMapTileGroup))]
    public class BaseMapTileGroupEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                // Only update the MapTiles when modifying it in the editor.
                return;
            }

            if (Selection.gameObjects.Length == 0)
            {
                return;
            }

            for (int index = 0; index < Selection.gameObjects.Length; index++)
            {
                GameObject gameObjectToModifiy = Selection.gameObjects[index];

                BaseMapTileGroup baseMapTileGroup = gameObjectToModifiy.GetComponent<BaseMapTileGroup>();

                if (baseMapTileGroup != null)
                {
                    baseMapTileGroup.Validate();
                }
            }
        }
    }
}
#endif