using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649
/// <summary>
/// Represents a parent object of a maptile prefab.
/// Controls which type of maptile is active as a child.
/// </summary>
public class BaseMapTile : MonoBehaviour
{
    [Serializable]
    private class MapTileTypeAssignment
    {
        public MapTileType m_MapTileType;
        public GameObject m_MapTilePrefab;
    }

    [SerializeField]
    private List<MapTileTypeAssignment> m_mapTileTypeAssignmentList;

    [SerializeField]
    private MapTileType m_mapTileType;
    public MapTileType MapTileType { get { return m_mapTileType; } set { m_mapTileType = value; } }

    private GameObject m_currentInstantiatedMapTile;
    private MapTileType m_currentInstantiatedMapTileType;

    /// <summary>
    /// Creates the first maptile child based on a default maptiletype.
    /// </summary>
    public void Initialize()
    {
        m_mapTileType = MapTileType.Water;

        Validate();
    }

    /// <summary>
    /// Validates the specified map tile type.
    /// If the maptiletype has changed or the child was not created, will create the correct maptile.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    public void Validate()
    {
        if(m_currentInstantiatedMapTileType == m_mapTileType && m_currentInstantiatedMapTile != null)
        {
            return;
        }

        if (m_currentInstantiatedMapTile != null)
        {
            DestroyImmediate(m_currentInstantiatedMapTile);
        }

        GameObject prefabToInstantiate = GetPrefabOfMapTileType(m_mapTileType);

        if(prefabToInstantiate != null)
        {
            m_currentInstantiatedMapTile = Instantiate(prefabToInstantiate);
            m_currentInstantiatedMapTile.transform.SetParent(this.transform);
            m_currentInstantiatedMapTile.transform.localPosition = Vector3.zero;

            m_currentInstantiatedMapTileType = m_mapTileType;
        }
        else
        {
            Debug.LogErrorFormat("MapTile with Type: '{0}' was not found!", m_mapTileType);

            m_currentInstantiatedMapTileType = MapTileType.Empty;
        }
    }

    /// <summary>
    /// Returns a prefab from the MapTileTypeAssignment based on the given MapTileType.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    /// <returns></returns>
    private GameObject GetPrefabOfMapTileType(MapTileType mapTileType)
    {
        MapTileTypeAssignment mapTileTypeAssignment = m_mapTileTypeAssignmentList.Find(prefab => prefab.m_MapTileType == mapTileType);

        return mapTileTypeAssignment == null ? null : mapTileTypeAssignment.m_MapTilePrefab;
    }
}
