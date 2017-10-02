/// <summary>
/// Will use a given UnitBalancingData instance to determine the movement costs and allowance of a unit.
/// </summary>
public class UnitBalancingMovementCostResolver : IMovementCostResolver
{
    private UnitBalancingData m_unitBalancingData;

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
    public bool HasUnitEnoughMovementRangeLeft(int currentMovementCost)
    {
        return currentMovementCost <= m_unitBalancingData.m_MovementRangePerRound;
    }

    /// <summary>
    /// Determines whether a unit can walk on a map tile.
    /// </summary>
    /// <param name="mapTile">The map tile instance to test against.</param>
    public bool CanUnitWalkOnMapTile(BaseMapTile mapTile)
    {
        return m_unitBalancingData.m_WalkableMapTileTypes.Exists(walkableMapTile => walkableMapTile.m_MapTileType == mapTile.MapTileType);
    }
}
