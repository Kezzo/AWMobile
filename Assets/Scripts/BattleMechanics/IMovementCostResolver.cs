/// <summary>
/// Classes that implement this interface can be injected into the pathfinding to handle different movement cost/movement allowance cases.
/// </summary>
public interface IMovementCostResolver
{
    /// <summary>
    /// Gets the type of the movement cost to walk on map tile.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    int GetMovementCostToWalkOnMapTileType(MapTileType mapTileType);

    /// <summary>
    /// Determines whether a unit enough movement range left to with the given current movement cost.
    /// </summary>
    /// <param name="currentMovementCost">The current movement cost.</param>
    bool HasUnitEnoughMovementRangeLeft(int currentMovementCost);

    /// <summary>
    /// Determines whether a unit can walk on the given map tile.
    /// </summary>
    /// <param name="mapTile">The map tile.</param>
    bool CanUnitWalkOnMapTile(BaseMapTile mapTile);
}
