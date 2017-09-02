#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor script to enable function of the MapTileGeneratorEditor class in the editor.
/// </summary>
[CanEditMultipleObjects]
[CustomEditor(typeof(MapTileGeneratorEditor))]
public class MapTileGeneratorEditorHelper : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        MapTileGeneratorEditor mapTileGeneratorEditor = (MapTileGeneratorEditor)target;

        GUILayout.Space(10f);

        if (GUILayout.Button("Generate Empty Map"))
        {
            mapTileGeneratorEditor.GenerateMap();
        }

        if (GUILayout.Button("Clear Hierarchy"))
        {
            mapTileGeneratorEditor.ClearMap();
        }

        GUILayout.Space(10f);

        if (GUILayout.Button("Load Map File"))
        {
            mapTileGeneratorEditor.LoadExistingMap(mapTileGeneratorEditor.LoadMapGenerationData());
        }

        if (GUILayout.Button("Update Map File"))
        {
            ControllerContainer.AssetDatabaseService.UpdateExistingAssetFile(mapTileGeneratorEditor.CurrentlyVisibleMap);
        }

        if (GUILayout.Button("Save New Map File"))
        {
            mapTileGeneratorEditor.SaveMapUnderLevelName();
        }
    }
}
#endif