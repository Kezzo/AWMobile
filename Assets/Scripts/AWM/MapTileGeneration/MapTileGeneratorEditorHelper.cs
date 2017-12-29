#if UNITY_EDITOR
using AWM.System;
using UnityEditor;
using UnityEngine;

namespace AWM.MapTileGeneration
{
    /// <summary>
    /// Editor script to enable function of the MapTileGeneratorEditor class in the editor.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(MapTileGeneratorEditor))]
    public class MapTileGeneratorEditorHelper : Editor
    {
        private MapTileGeneratorEditor m_mapTileGeneratorEditor;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (m_mapTileGeneratorEditor == null)
            {
                m_mapTileGeneratorEditor = (MapTileGeneratorEditor)target;
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("Generate Empty Map"))
            {
                m_mapTileGeneratorEditor.GenerateMap();
            }

            if (GUILayout.Button("Clear Hierarchy"))
            {
                m_mapTileGeneratorEditor.ClearMap();
            }

            GUILayout.Space(10f);

            if (GUILayout.Button("Load Map File"))
            {
                m_mapTileGeneratorEditor.LoadExistingMap(m_mapTileGeneratorEditor.LoadMapGenerationData());
            }

            if (GUILayout.Button("Update Map File"))
            {
                CC.ADS.UpdateExistingAssetFile(m_mapTileGeneratorEditor.CurrentlyVisibleMap);
            }

            if (GUILayout.Button("Save New Map File"))
            {
                m_mapTileGeneratorEditor.SaveMapUnderLevelName();
            }

            GUILayout.Space(10f);

            m_mapTileGeneratorEditor.HotkeysActive = GUILayout.Toggle(m_mapTileGeneratorEditor.HotkeysActive, "Toggle hotkeys");
        }
    }
}
#endif