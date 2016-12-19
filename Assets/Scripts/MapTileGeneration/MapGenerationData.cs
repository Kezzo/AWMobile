using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Model to save needed data to generate a map and the units on it.
/// </summary>
public class MapGenerationData : ScriptableObject
{
    public string m_LevelName;
    public Vector2 m_LevelSize;
    public float m_MapTileMargin;

    public int m_MapTileGroupSize;
    public List<MapTileGroup> m_MapTileGroups;

    [Serializable]
    public class MapTileGroup
    {
        public Vector2 m_GroupPositionVector;
        public Vector3 m_GroupPosition;
        public List<MapTile> m_MapTiles;

        /// <summary>
        /// Gets the map tile at a group vector.
        /// </summary>
        /// <param name="mapVector">The map vector.</param>
        /// <returns></returns>
        public MapTile GetMapTileAtGroupVector(Vector2 mapVector)
        {
            if (m_MapTiles == null || m_MapTiles.Count == 0)
            {
                return null;
            }

            return m_MapTiles.Find(mapTile => mapTile.m_PositionVector == mapVector);
        }
    }

    [Serializable]
    public class MapTile
    {
        public Vector2 m_PositionVector;
        public Vector3 m_LocalPositionInGroup;

        // Store MapTileType and UnitType with int, because enums cannot be serialized correctly or Unity is buggy in that regard...
        public int m_MapTileTypeAsInt;
        public MapTileType MapTileType
        {
            get { return (MapTileType) m_MapTileTypeAsInt; }
            set { m_MapTileTypeAsInt = (int) value; }
        }

        public int m_UnitTypeAsInt;
        public UnitType UnitType
        {
            get { return (UnitType) m_UnitTypeAsInt; }
            set { m_UnitTypeAsInt = (int) value; }
        }

    }

    /// <summary>
    /// Gets the map tile group at a map vector.
    /// </summary>
    /// <param name="mapVector">The map vector.</param>
    /// <returns></returns>
    public MapTileGroup GetMapTileGroupAtMapVector(Vector2 mapVector)
    {
        if (m_MapTileGroups == null || m_MapTileGroups.Count == 0)
        {
            return null;
        }

        return m_MapTileGroups.Find(mapTileGroup => mapTileGroup.m_GroupPositionVector == mapVector);
    }
}
