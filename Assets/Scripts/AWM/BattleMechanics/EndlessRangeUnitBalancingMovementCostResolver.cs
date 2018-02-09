using AWM.MapTileGeneration;
using AWM.Models;
using AWM.System;
using UnityEngine;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// A <see cref="UnitBalancingData"/> based movement cost resolver that ignores movement ranges of units and 
    /// implements specific implementation to enable AI controlled units to find the best route to move in a turn.
    /// </summary>
    /// <seealso cref="AWM.BattleMechanics.UnitBalancingMovementCostResolver" />
    public class EndlessRangeUnitBalancingMovementCostResolver : UnitBalancingMovementCostResolver
    {
        private readonly BaseMapTile m_targetBaseMapTile;

        public EndlessRangeUnitBalancingMovementCostResolver(UnitBalancingData unitBalancing, Vector2 targetNode) : base(unitBalancing)
        {
            m_targetBaseMapTile = CC.TNC.GetMapTile(targetNode);
        }

        /// <summary>
        /// Will just ignore the <see cref="UnitBalancingData.m_MovementRangePerRound"/> and return true.
        /// This is needed to calculate a potential future route.
        /// </summary>
        /// <param name="currentMovementCost">The current movement cost of the unit.</param>
        public override bool HasUnitEnoughMovementRangeLeft(int currentMovementCost)
        {
            return true;
        }

        /// <summary>
        /// Determines whether a unit can walk on a map tile.
        /// </summary>
        /// <param name="mapTile">The map tile instance to test against.</param>
        /// <param name="movementCostToMapTile">
        /// The movement cost required to walk to the given maptile. 
        /// Used to ignore unit collision when a maptile was given that can't be reached based on the balanced movement range.
        /// </param>
        /// <param name="allowPassability">If set to true, this method will return true for a given maptile if the unit of this class only pass over it.</param>
        public override bool CanUnitWalkOnMapTile(BaseMapTile mapTile, int movementCostToMapTile, bool allowPassability = false)
        {
            if (mapTile == m_targetBaseMapTile)
            {
                return true;
            }

            bool canUnitWalkOnMapTileType = CanUnitWalkOnMapTile(mapTile);

            bool canPassUnitMetaTypeOnTile = true;

            // Only checking unit collision for tiles in movement range here to allow finding a route which could be unblocked in future turns.
            // If this wouldn't be done, unit movement can be completely blocked when a choke point, far away from the moving unit, is blocked.
            if (movementCostToMapTile <= UnitBalancingData.m_MovementRangePerRound)
            {
                if (allowPassability)
                {
                    BaseUnit unitOnTile = CC.BSC.GetUnitOnNode(mapTile.m_SimplifiedMapPosition);

                    canPassUnitMetaTypeOnTile = unitOnTile == null ||
                        UnitBalancingData.m_PassableUnitMetaTypes.Contains(unitOnTile.GetUnitBalancing().m_UnitMetaType);
                }
                else
                {
                    canPassUnitMetaTypeOnTile = !CC.BSC.IsUnitOnNode(mapTile.m_SimplifiedMapPosition);
                }
            }

            return canUnitWalkOnMapTileType && canPassUnitMetaTypeOnTile;
        }
    }
}
