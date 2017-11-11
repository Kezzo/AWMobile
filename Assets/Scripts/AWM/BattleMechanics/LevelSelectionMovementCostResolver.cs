using AWM.Enums;
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
        /// <param name="mapTileType">Type of the map tile.</param>
        public int GetMovementCostToWalkOnMapTileType(MapTileType mapTileType)
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
        public bool CanUnitWalkOnMapTile(BaseMapTile mapTile)
        {
            return mapTile.LevelSelectionRouteType == LevelSelectionRouteType.LevelSelectionRoute ||
                   mapTile.LevelSelectionRouteType == LevelSelectionRouteType.LevelSelector;
        }
    }
}
