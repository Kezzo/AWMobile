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

        if (m_mapTileType == MapTileType.Empty)
        {
            return;
        }

        GameObject prefabToInstantiate = FindObjectOfType<MapTileGeneratorEditor>().GetPrefabOfMapTileType(m_mapTileType);

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
}
