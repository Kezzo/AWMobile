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
        public List<MapTileType> m_WalkableMapTileTypes;
    }

    [SerializeField]
    private List<UnitBalancing> m_unitBalancingList;

    /// <summary>
    /// Gets the unit balancing.
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
