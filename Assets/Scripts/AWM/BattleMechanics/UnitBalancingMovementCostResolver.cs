using AWM.Enums;
using AWM.MapTileGeneration;
using AWM.Models;
using AWM.System;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// Will use a given UnitBalancingData instance to determine the movement costs and allowance of a unit.
    /// </summary>
    public class UnitBalancingMovementCostResolver : IMovementCostResolver
    {
        private readonly UnitBalancingData m_unitBalancingData;
        protected UnitBalancingData UnitBalancingData { get { return m_unitBalancingData; } }

        public UnitBalancingMovementCostResolver(UnitBalancingData unitBalancing)
        {
            m_unitBalancingData = unitBalancing;
        }

        /// <summary>
        /// Gets the movement cost to walk on a map tile with a certain type.
        /// </summary>
        /// <param name="mapTileType">Type of the map tile.</param>
        /// <returns></returns>
        public int GetMovementCostToWalkOnMapTileType(MapTileType mapTileType)
        {
            var walkMapToCheck = m_unitBalancingData.m_WalkableMapTileTypes.Find(walkableMapTile => walkableMapTile.m_MapTileType == mapTileType);

            return walkMapToCheck == null ? 0 : walkMapToCheck.m_MovementCost;
        }

        /// <summary>
        /// Determines whether a unit has enough movement range left based on the given current movement cost.
        /// </summary>
        /// <param name="currentMovementCost">The current movement cost.</param>
        public virtual bool HasUnitEnoughMovementRangeLeft(int currentMovementCost)
        {
            return currentMovementCost <= m_unitBalancingData.m_MovementRangePerRound;
        }

        /// <summary>
        /// Determines whether a unit can walk on a map tile.
        /// </summary>
        /// <param name="mapTile">The map tile instance to test against.</param>
        /// <param name="movementCostToMapTile">
        /// The movement cost required to walk to the given maptile. 
        /// Used by implementations that want to change the return result based on the movement cost reachability.
        /// </param>
        /// <param name="allowPassability">If set to true, this method will return true for a given maptile if the unit of this class only pass over it.</param>
        public virtual bool CanUnitWalkOnMapTile(BaseMapTile mapTile, int movementCostToMapTile, bool allowPassability = false)
        {
            bool canUnitWalkOnMapTileType = CanUnitWalkOnMapTile(mapTile);

            bool canPassUnitMetaTypeOnTile;

            if (allowPassability)
            {
                BaseUnit unitOnTile = CC.BSC.GetUnitOnNode(mapTile.m_SimplifiedMapPosition);

                canPassUnitMetaTypeOnTile = unitOnTile == null ||
                    m_unitBalancingData.m_PassableUnitMetaTypes.Contains(unitOnTile.GetUnitBalancing().m_UnitMetaType);
            }
            else
            {
                canPassUnitMetaTypeOnTile = !CC.BSC.IsUnitOnNode(mapTile.m_SimplifiedMapPosition);
            }

            return canUnitWalkOnMapTileType && canPassUnitMetaTypeOnTile;
        }

        /// <summary>
        /// Determines whether a unit can unit walk on a map tile depending on the <see cref="MapTileType"/> and if it has a street.
        /// </summary>
        /// <param name="mapTile">The map tile to check.</param>
        protected bool CanUnitWalkOnMapTile(BaseMapTile mapTile)
        {
            bool canUnitWalkOnMapTileType = false;

            UnitBalancingData.WalkableMapTiles walkableMapTileToCheck = m_unitBalancingData.m_WalkableMapTileTypes.Find(
                walkableMapTile => walkableMapTile.m_MapTileType == mapTile.MapTileType);

            if (walkableMapTileToCheck != null)
            {
                canUnitWalkOnMapTileType = mapTile.HasStreet ? walkableMapTileToCheck.m_CanWalkOnStreet : walkableMapTileToCheck.m_CanWalkOnTile;
            }

            return canUnitWalkOnMapTileType;
        }
    }
}
