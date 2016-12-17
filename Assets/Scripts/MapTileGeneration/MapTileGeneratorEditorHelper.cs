﻿#if UNITY_EDITOR
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

        if (GUILayout.Button("Generate"))
        {
            mapTileGeneratorEditor.GenerateMap();
        }

        if (GUILayout.Button("Clear"))
        {
            mapTileGeneratorEditor.ClearMap();
        }
    }
}
#endif