using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to generation a map from maptiles in the editor.
/// </summary>
public class MapTileGeneratorEditor : MonoBehaviour
{
    [SerializeField]
    private Vector2 m_levelSize;

    [SerializeField]
    private float m_tileMargin;

    [SerializeField]
    private GameObject m_tilePrefab;

    [SerializeField]
    private Transform m_levelRoot;

    [Serializable]
    private class MapTileTypeAssignment
    {
#pragma warning disable 649
        public MapTileType m_MapTileType;
        public GameObject m_MapTilePrefab;
#pragma warning restore 649
    }

    [SerializeField]
    private List<MapTileTypeAssignment> m_mapTileTypeAssignmentList;

    /// <summary>
    /// Generates the map.
    /// </summary>
    public void GenerateMap()
    {
        ClearMap();

        ControllerContainer.MapTileGenerator.GenerateGroups(m_levelSize, m_tileMargin, m_tilePrefab, m_levelRoot);
    }

    /// <summary>
    /// Clears the previous generation.
    /// </summary>
    public void ClearMap()
    {
        List<GameObject> gameObjectsToKill = new List<GameObject>();

        foreach (Transform child in m_levelRoot)
        {
            gameObjectsToKill.Add(child.gameObject);
        }

        for (int i = 0; i < gameObjectsToKill.Count; i++)
        {
            DestroyImmediate(gameObjectsToKill[i]);
        }
    }

    /// <summary>
    /// Returns a prefab from the MapTileTypeAssignment based on the given MapTileType.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    /// <returns></returns>
    public GameObject GetPrefabOfMapTileType(MapTileType mapTileType)
    {
        MapTileTypeAssignment mapTileTypeAssignment = m_mapTileTypeAssignmentList.Find(prefab => prefab.m_MapTileType == mapTileType);

        return mapTileTypeAssignment == null ? null : mapTileTypeAssignment.m_MapTilePrefab;
    }
}
