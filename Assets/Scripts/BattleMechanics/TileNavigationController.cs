using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to provide tile navigation algorithms results for units.
/// This controller also implements the A* pathfinding algorithm.
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
    /// Gets the map tile on a given simplified position.
    /// </summary>
    /// <param name="mapTilePosition">The map tile position.</param>
    /// <returns></returns>
    public BaseMapTile GetMapTile(Vector2 mapTilePosition)
    {
        BaseMapTile baseMapTile = null;

        m_mapTilePositions.TryGetValue(mapTilePosition, out baseMapTile);

        return baseMapTile;
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

        SimpleUnitBalancing.UnitBalancing unitBalancing = unitToCheckFor.GetUnitBalancing();

        foreach (var mapTilesPosition in m_mapTilePositions)
        {
            if (!unitBalancing.CanUnitWalkOnMapTileType(mapTilesPosition.Value.MapTileType))
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
    /// Gets the distance to a map tile.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <returns></returns>
    private int GetDistanceToCoordinate(Vector2 from, Vector2 to)
    {
        Vector2 directionalVectorToMapTile = from - to;

        return (int) (Mathf.Abs(directionalVectorToMapTile.x) + Mathf.Abs(directionalVectorToMapTile.y));
    }

    /// <summary>
    /// Gets the best way from a unit to a maptile.
    /// The best way is shortest walkable way to the maptile.
    /// </summary>
    /// <param name="unitToMove">The unit to move.</param>
    /// <param name="destinationMapTile">The destination map tile.</param>
    /// <returns></returns>
    public List<Vector2> GetBestWayToDestination(BaseUnit unitToMove, BaseMapTile destinationMapTile)
    {
        Vector2 start = unitToMove.CurrentSimplifiedPosition;
        Vector2 destination = destinationMapTile.SimplifiedMapPosition;
        SimpleUnitBalancing.UnitBalancing unitBalancing = unitToMove.GetUnitBalancing();

        PriorityQueue<Vector2> queueOfNodesToCheck = new PriorityQueue<Vector2>();
        queueOfNodesToCheck.Enqueue(start, 0);

        // node; from
        Dictionary<Vector2, Vector2> routeMapping = new Dictionary<Vector2, Vector2>();
        // Also stores which nodes were already checked, entries are updated when a better way to a node was found.
        Dictionary<Vector2, int> costToMoveToNodeStorage = new Dictionary<Vector2, int>();

        while (!queueOfNodesToCheck.IsEmpty)
        {
            Vector2 nodeToGetNeighboursFrom = queueOfNodesToCheck.Dequeue();

            if (nodeToGetNeighboursFrom == destination)
            {
                // Destination found!
                break;
            }

            List<Vector2> adjacentNodes = GetWalkableAdjacentNodes(nodeToGetNeighboursFrom, unitBalancing);

            int costToGetToPreviousMapTile = 0;
            costToMoveToNodeStorage.TryGetValue(nodeToGetNeighboursFrom, out costToGetToPreviousMapTile);

            for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
            {
                Vector2 nodeToCheck = adjacentNodes[nodeIndex];
                int costToMoveToNode = costToGetToPreviousMapTile +
                                       unitBalancing.GetMovementCostToWalkOnMapTileType(destinationMapTile.MapTileType);

                int existingCostToMoveToNode = 0;

                // Only add node, if it wasn't added before or if a short path to the node was found.
                if (!costToMoveToNodeStorage.ContainsKey(nodeToCheck) ||
                    (costToMoveToNodeStorage.TryGetValue(nodeToCheck, out existingCostToMoveToNode) &&
                    costToMoveToNode < existingCostToMoveToNode))
                {
                    routeMapping.Add(nodeToCheck, nodeToGetNeighboursFrom);
                    costToMoveToNodeStorage.Add(nodeToCheck, costToMoveToNode);

                    queueOfNodesToCheck.Enqueue(nodeToCheck, costToMoveToNode + GetDistanceToCoordinate(nodeToCheck, destination));
                }
            }
        }

        return GetRouteListFromRouteMapping(start, destination, routeMapping);
    }

    /// <summary>
    /// Gets a route list from a route mapping dictionary.
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routeMapping">The route mapping.</param>
    /// <returns></returns>
    private List<Vector2> GetRouteListFromRouteMapping(Vector2 start, Vector2 destination, Dictionary<Vector2, Vector2> routeMapping)
    {
        List<Vector2> bestWayToDestination = new List<Vector2>();

        Vector2 nodeToCheckNext = destination;
        bestWayToDestination.Add(destination);

        while (true)
        {
            if (nodeToCheckNext == start)
            {
                break;
            }

            if (routeMapping.TryGetValue(nodeToCheckNext, out nodeToCheckNext))
            {
                bestWayToDestination.Add(nodeToCheckNext);
            }
        }

        bestWayToDestination.Reverse();

        return bestWayToDestination;
    }

    /// <summary>
    /// Gets the adjacent tiles of a source tile position.
    /// This method considers only the previously registered maptiles.
    /// </summary>
    /// <param name="source">The source.</param>
    /// <param name="unitBalancing">The unit balancing.</param>
    /// <returns></returns>
    private List<Vector2> GetWalkableAdjacentNodes(Vector2 source, SimpleUnitBalancing.UnitBalancing unitBalancing)
    {
        List<Vector2> adjacentNodes = new List<Vector2>();

        Vector2[] adjacentModifier =
        {
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(-1, 0),
            new Vector2(0, -1)
        };

        for (int modifierIndex = 0; modifierIndex < adjacentModifier.Length; modifierIndex++)
        {
            Vector2 adjacentTile = source + adjacentModifier[modifierIndex];

            BaseMapTile adjacenBaseMapTile;

            // Is tile position registered?
            if (m_mapTilePositions.TryGetValue(adjacentTile, out adjacenBaseMapTile) &&
                // Is MapTile walkable by unit?
                unitBalancing.CanUnitWalkOnMapTileType(adjacenBaseMapTile.MapTileType) &&
                // Is there a unit on the positon? (rendering it unwalkable)
                !m_registeredUnits.Exists(unit => unit.CurrentSimplifiedPosition == adjacentTile))
            {
                adjacentNodes.Add(adjacentTile);
            }
        }

        return adjacentNodes;
    }
}
