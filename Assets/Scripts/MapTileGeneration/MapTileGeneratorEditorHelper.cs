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

        if (GUILayout.Button("Clear"))
        {
            mapTileGeneratorEditor.ClearMap();
        }

        GUILayout.Space(10f);

        if (GUILayout.Button("Load Map"))
        {
            mapTileGeneratorEditor.LoadExistingMap();
        }

        if (GUILayout.Button("Save Map"))
        {
            mapTileGeneratorEditor.SaveMapUnderLevelName();
        }
    }
}
#endif