﻿using AWM.Enums;
using AWM.MapTileGeneration;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// Will define if the level selection unit can be moved.
    /// Will ignore movement cost, because that's not used in the level selection.
    /// </summary>
    public class LevelSelectionMovementCostResolver : IMovementCostResolver
    {
        /// <summary>
        /// Always returning 0 here, because there is no movement cost in the level selection.
        /// </summary>
        /// <param name="mapTile">The maptile to get the movement cost from.</param>
        public int GetMovementCostToWalkOnMapTile(BaseMapTile mapTile)
        {
            return 0;
        }

        /// <summary>
        /// A unit in the level selection always has enough movement range left.
        /// </summary>
        /// <param name="currentMovementCost">The current movement cost.</param>
        public bool HasUnitEnoughMovementRangeLeft(int currentMovementCost)
        {
            return true;
        }

        /// <summary>
        /// Determines whether a unit in the level selection can walk on the given MapTile.
        /// </summary>
        /// <param name="mapTile">The map tile.</param>
        /// /// <param name="movementCostToMapTile">
        /// The movement cost required to walk to the given maptile. 
        /// Used by implementations that want to change the return result based on the movement cost reachability.
        /// </param>
        /// <param name="allowPassability">Ignored in this implementation.</param>
        public bool CanUnitWalkOnMapTile(BaseMapTile mapTile, int movementCostToMapTile, bool allowPassability = false)
        {
            return mapTile.LevelSelectionRouteType == LevelSelectionRouteType.LevelSelectionRoute ||
                   mapTile.LevelSelectionRouteType == LevelSelectionRouteType.LevelSelector;
        }
    }
}
