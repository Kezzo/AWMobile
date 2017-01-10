#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor script to enable function of the BaseMapTile class in the editor.
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(BaseMapTile))]
public class BaseMapTileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (Application.isPlaying)
        {
            // Only update the MapTile and unit when modifying it in the editor.
            return;
        }

        if (Selection.gameObjects.Length == 0)
        {
            return;
        }

        for (int index = 0; index < Selection.gameObjects.Length; index++)
        {
            GameObject gameObjectToModifiy = Selection.gameObjects[index];

            BaseMapTile baseMapTile = gameObjectToModifiy.GetComponent<BaseMapTile>();

            if (baseMapTile != null)
            {
                baseMapTile.ValidateMapTile();
                baseMapTile.ValidateUnitType();
            }
        }
    }

}
#endif