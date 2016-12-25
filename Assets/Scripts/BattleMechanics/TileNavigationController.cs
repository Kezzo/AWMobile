using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to provide tile navigation algorithms results for units.
/// </summary>
public class TileNavigationController
{
    private Dictionary<Vector2, BaseMapTile> m_mapTilePositions;
    private List<BaseUnit> m_registeredUnits;

    /// <summary>
    /// Initializes the TileNavigationController.
    /// </summary>
    /// <param name="mapSize">Size of the map.</param>
    public void Initialize(Vector2 mapSize)
    {
        m_mapTilePositions = new Dictionary<Vector2, BaseMapTile>((int) (mapSize.x * mapSize.y));
        m_registeredUnits = new List<BaseUnit>();
    }

    /// <summary>
    /// Registers a map tile on a position.
    /// </summary>
    /// <param name="mapTilePosition">The map tile position.</param>
    /// <param name="baseMapTile">The base map tile.</param>
    public void RegisterMapTile(Vector2 mapTilePosition, BaseMapTile baseMapTile)
    {
        if (m_mapTilePositions.ContainsKey(mapTilePosition))
        {
            Debug.LogErrorFormat("Tried to register a maptile under an already registered coordinate: '{0}'. " +
                                 "Not registering the second MapTile!", mapTilePosition);
        }
        else
        {
            m_mapTilePositions.Add(mapTilePosition, baseMapTile);
        }
    }

    /// <summary>
    /// Registers the unit.
    /// </summary>
    /// <param name="baseUnit">The base unit.</param>
    public void RegisterUnit(BaseUnit baseUnit)
    {
        m_registeredUnits.Add(baseUnit);
    }

    /// <summary>
    /// Shows the movement fields for unit.
    /// </summary>
    /// <param name="unitToCheckFor">The unit.</param>
    public List<BaseMapTile> GetWalkableMapTiles(BaseUnit unitToCheckFor)
    {
        List<BaseMapTile> walkableMapTiles = new List<BaseMapTile>();

        SimpleUnitBalancing.UnitBalancing unitBalancing =
            Root.Instance.SimeSimpleUnitBalancing.GetUnitBalancing(unitToCheckFor.UnitType);

        foreach (var mapTilesPosition in m_mapTilePositions)
        {
            if (!unitBalancing.m_WalkableMapTileTypes.Contains(mapTilesPosition.Value.MapTileType))
            {
                // MapTileType is not walkable by this unit.
                continue;
            }

            int distanceToMapTile = GetDistanceToCoordinate(mapTilesPosition.Key, unitToCheckFor.CurrentSimplifiedPosition);

            if (distanceToMapTile > unitBalancing.m_MovementSpeed)
            {
                // MapTileType is too far away.
                continue;
            }

            if (m_registeredUnits.Exists(unit => unit.CurrentSimplifiedPosition == mapTilesPosition.Key))
            {
                // There already is a unit on this tile.
                continue;
            }

            walkableMapTiles.Add(mapTilesPosition.Value);
        }

        return walkableMapTiles;
    }

    /// <summary>
    /// Gets the distance to map tile.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <returns></returns>
    private int GetDistanceToCoordinate(Vector2 from, Vector2 to)
    {
        Vector2 directionalVectorToMapTile = from - to;

        return (int) (Mathf.Abs(directionalVectorToMapTile.x) + Mathf.Abs(directionalVectorToMapTile.y));
    }
}
