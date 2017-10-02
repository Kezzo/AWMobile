using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitBalancingData : ScriptableObject
{
    public UnitType m_UnitType;
    public UnitMetaType m_UnitMetaType;

    // Tiles the unit can move in one turn.
    public int m_MovementRangePerRound;
    public List<WalkableMapTiles> m_WalkableMapTileTypes;

    [Serializable]
    public class WalkableMapTiles
    {
        public MapTileType m_MapTileType;
        public int m_MovementCost;
    }

    public int m_Health;

    // TODO: Implement min and max attack range for units that can only attack units far away.
    public int m_AttackRange;

    public List<UnitMetaType> m_AttackableUnitMetaTypes;
    public List<DamageOnUnitType> m_DamageOnUnitsList;

    [Serializable]
    public class DamageOnUnitType
    {
        public UnitType m_UnitType;
        public int m_Damage;
    }

    /// <summary>
    /// Gets the type of the damage on unit.
    /// </summary>
    /// <param name="unitType">Type of the unit.</param>
    /// <returns></returns>
    public int GetDamageOnUnitType(UnitType unitType)
    {
        DamageOnUnitType damageOnUnitType = m_DamageOnUnitsList.Find(damageValue => damageValue.m_UnitType == unitType);

        return damageOnUnitType == null ? 0 : damageOnUnitType.m_Damage;
    }
}
