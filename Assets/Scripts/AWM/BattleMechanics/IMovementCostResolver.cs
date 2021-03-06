﻿using AWM.Enums;
using AWM.MapTileGeneration;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// Classes that implement this interface can be injected into the pathfinding to handle different movement cost/movement allowance cases.
    /// </summary>
    public interface IMovementCostResolver
    {
        /// <summary>
        /// Gets the type of the movement cost to walk on map tile.
        /// </summary>
        /// <param name="mapTile">The maptile to get the movement cost from.</param>
        int GetMovementCostToWalkOnMapTile(BaseMapTile mapTile);

        /// <summary>
        /// Determines whether a unit enough movement range left to with the given current movement cost.
        /// </summary>
        /// <param name="currentMovementCost">The current movement cost.</param>
        bool HasUnitEnoughMovementRangeLeft(int currentMovementCost);

        /// <summary>
        /// Determines whether a unit can walk on the given map tile.
        /// </summary>
        /// <param name="mapTile">The map tile instance to test against.</param>
        /// <param name="movementCostToMapTile">
        /// The movement cost required to walk to the given maptile. 
        /// Used by implementations that want to change the return result based on the movement cost reachability.
        /// </param>
        /// <param name="allowPassability">If set to true, this method will return true for a given maptile if the unit of this class only pass over it.</param>
        bool CanUnitWalkOnMapTile(BaseMapTile mapTile, int movementCostToMapTile, bool allowPassability = false);
    }
}
