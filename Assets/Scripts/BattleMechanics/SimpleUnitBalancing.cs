using System;
using System.Collections.Generic;
using UnityEngine;

public class SimpleUnitBalancing : MonoBehaviour
{
    // Put this data into ScriptableObject
    [Serializable]
    public class UnitBalancing
    {
        public UnitType m_UnitType;

        // Tiles the unit can move in one turn.
        public int m_MovementSpeed;
        public List<WalkableMapTiles> m_WalkableMapTileTypes;

        [Serializable]
        public class WalkableMapTiles
        {
            public MapTileType m_MapTileType;
            public int m_MovementCost;
        }

        /// <summary>
        /// Determines whether a unit can walk on a map tile type.
        /// </summary>
        /// <param name="mapTileType">Type of the map tile.</param>
        /// <returns></returns>
        public bool CanUnitWalkOnMapTileType(MapTileType mapTileType)
        {
            return m_WalkableMapTileTypes.Exists(walkableMapTile => walkableMapTile.m_MapTileType == mapTileType);
        }

        /// <summary>
        /// Gets the movement cost to walk on a map tile with a certain type.
        /// </summary>
        /// <param name="mapTileType">Type of the map tile.</param>
        /// <returns></returns>
        public int GetMovementCostToWalkOnMapTileType(MapTileType mapTileType)
        {
            var walkMapToCheck = m_WalkableMapTileTypes.Find(walkableMapTile => walkableMapTile.m_MapTileType == mapTileType);

            return walkMapToCheck == null ? 0 : walkMapToCheck.m_MovementCost;
        }
    }

    [SerializeField]
    private List<UnitBalancing> m_unitBalancingList;

    /// <summary>
    /// Gets a unit balancing based on the UnitType.
    /// </summary>
    /// <param name="unitType">Type of the unit.</param>
    /// <returns></returns>
    public UnitBalancing GetUnitBalancing(UnitType unitType)
    {
        UnitBalancing unitBalancingToReturn = m_unitBalancingList.Find(unitBalancing => unitBalancing.m_UnitType == unitType);

        if (unitBalancingToReturn == null)
        {
            Debug.LogErrorFormat("UnitBalancing for UnitType: '{0}' was not found!", unitType);
        }

        return unitBalancingToReturn;
    }
}
