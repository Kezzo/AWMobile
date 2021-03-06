﻿using System;
using System.Collections.Generic;
using AWM.Enums;
using AWM.MapTileGeneration;
using UnityEngine;

namespace AWM.Models
{
    /// <summary>
    /// Model to save needed data to generate a map and the units on it.
    /// </summary>
    public class MapGenerationData : ScriptableObject, IMapTileProvider
    {
        public string m_LevelName;
        public bool m_IsLevelSelection;

        public Team[] m_Teams;

        public Vector2 m_LevelSize;
        public float m_MapTileMargin;

        public Vector3 m_MaxCameraPosition;
        public Vector3 m_MinCameraPosition;

        public int m_MapTileGroupSize;
        public List<MapTileGroup> m_MapTileGroups;

        public MapCloudShadowData m_MapCloudShadowData;

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
            public MapTile GetMapTileAtGroupPosition(Vector2 mapVector)
            {
                if (m_MapTiles == null || m_MapTiles.Count == 0)
                {
                    return null;
                }

                return m_MapTiles.Find(mapTile => mapTile.m_PositionVector == mapVector);
            }

            /// <summary>
            /// Gets the simplified position on map.
            /// </summary>
            /// <param name="mapTileGroupSize">Size of the map tile group.</param>
            /// <returns></returns>
            public Vector2 GetSimplifiedPositionOnMap(int mapTileGroupSize)
            {
                return new Vector2((int) (m_GroupPositionVector.x * mapTileGroupSize), m_GroupPositionVector.y * mapTileGroupSize);
            }
        }

        [Serializable]
        public class MapTile
        {
            public Vector2 m_PositionVector;
            public Vector3 m_LocalPositionInGroup;

            public MapTileType m_MapTileType;
            public bool m_HasStreet;

            public Unit m_Unit;

            public LevelSelectionRouteType m_LevelSelectionRouteType;
            public string m_LevelNameToStart;
            public int m_LevelSelectionOrder;
            public Vector3 m_CenteredCameraPosition;

            public override string ToString()
            {
                return string.Format("PositionVector: '{0}', LocalPositionInGroup: '{1}', MapTileType: '{2}'",
                    m_PositionVector, m_LocalPositionInGroup, m_MapTileType);
            }
        }

        [Serializable]
        public class Unit
        {
            public UnitType m_UnitType;
            public TeamColor m_TeamColor;
            public CardinalDirection m_Orientation;
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

        /// <summary>
        /// Gets the position on map outside of the group.
        /// This is the position if we would see every maptile as its own entity on the map.
        /// </summary>
        /// <param name="mapTile">The map tile to check against.</param>
        /// <returns></returns>
        public Vector2 GetSimplifiedMapTilePosition(MapTile mapTile)
        {
            MapTileGroup mapTileGroupOfMapTile =
                m_MapTileGroups.Find(mapTileGroup => mapTileGroup.m_MapTiles.Contains(mapTile));

            if (mapTileGroupOfMapTile == null)
            {
                Debug.LogErrorFormat("Wasn't able to get MapTileGroup for MapTile: '{0}'", mapTile);
                return Vector2.zero;
            }

            Vector2 simplifiedMapTileGroupPosition = mapTileGroupOfMapTile.GetSimplifiedPositionOnMap(m_MapTileGroupSize);

            Vector2 simplifiedMapTilePositon = new Vector2(
                (int) (simplifiedMapTileGroupPosition.x + mapTile.m_PositionVector.x), 
                (int)(simplifiedMapTileGroupPosition.y + mapTile.m_PositionVector.y));

            return simplifiedMapTilePositon;
        }

        /// <summary>
        /// Returns the generation data of a map tile at the given position.
        /// </summary>
        public MapTile GetMapTileAtPosition(Vector2 position)
        {
            foreach (var mapTileGroup in m_MapTileGroups)
            {
                MapTile mapTile = mapTileGroup.m_MapTiles.Find(tile => GetSimplifiedMapTilePosition(tile) == position);

                if (mapTile != null)
                {
                    return mapTile;
                }
            }

            return null;
        }
    }
}
