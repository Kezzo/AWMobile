using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Class to provide tile navigation algorithms results for units.
/// This controller also implements the A* pathfinding algorithm.
/// </summary>
public class TileNavigationController
{
    private Dictionary<Vector2, BaseMapTile> m_mapTilePositions;

    /// <summary>
    /// Initializes the TileNavigationController.
    /// </summary>
    /// <param name="mapSize">Size of the map.</param>
    public void Initialize(Vector2 mapSize)
    {
        m_mapTilePositions = new Dictionary<Vector2, BaseMapTile>((int) (mapSize.x * mapSize.y));
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
    /// Shows the movement fields for unit.
    /// </summary>
    /// <param name="unitToCheckFor">The unit.</param>
    public List<BaseMapTile> GetWalkableMapTiles(BaseUnit unitToCheckFor)
    {
        SimpleUnitBalancing.UnitBalancing unitBalancing = unitToCheckFor.GetUnitBalancing();

        Queue<Vector2> nodesToCheck = new Queue<Vector2>();
        nodesToCheck.Enqueue(unitToCheckFor.CurrentSimplifiedPosition);

        Dictionary<Vector2, int> costToMoveToNodes = new Dictionary<Vector2, int>();

        while (true)
        {
            Vector2 nodeToCheck = nodesToCheck.Dequeue();

            int walkingCostToNode;
            costToMoveToNodes.TryGetValue(nodeToCheck, out walkingCostToNode);

            List<Vector2> adjacentNodes = GetWalkableAdjacentNodes(nodeToCheck, unitBalancing, walkingCostToNode);

            for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
            {
                Vector2 adjacentNode = adjacentNodes[nodeIndex];
                BaseMapTile baseMapTile;

                if (!m_mapTilePositions.TryGetValue(adjacentNode, out baseMapTile))
                {
                    Debug.LogErrorFormat("BaseMapTile on position: '{0}' was not found!", adjacentNode);
                    continue;
                }

                int costToMoveToNode = walkingCostToNode +
                                       unitBalancing.GetMovementCostToWalkOnMapTileType(baseMapTile.MapTileType);

                // Only add node, if it wasn't added before or if a shorter path to the node was found.
                if (!costToMoveToNodes.ContainsKey(adjacentNode))
                {
                    costToMoveToNodes.Add(adjacentNode, costToMoveToNode);
                    nodesToCheck.Enqueue(adjacentNode);
                }
                else
                {
                    var existingCostToMoveToNode = 0;
                    costToMoveToNodes.TryGetValue(adjacentNode, out existingCostToMoveToNode);

                    if (costToMoveToNode < existingCostToMoveToNode)
                    {
                        costToMoveToNodes[adjacentNode] = costToMoveToNode;
                        nodesToCheck.Enqueue(adjacentNode);
                    }
                }
            }

            if (nodesToCheck.Count == 0)
            {
                // All walkable nodes checked.
                break;
            }
        }

        // Get BaseMapTiles from Nodes.
        List<BaseMapTile> walkableMapTiles = new List<BaseMapTile>(costToMoveToNodes.Count);

        foreach (KeyValuePair<Vector2, int> walkableNode in costToMoveToNodes)
        {
            BaseMapTile baseMapTile;

            if (m_mapTilePositions.TryGetValue(walkableNode.Key, out baseMapTile))
            {
                walkableMapTiles.Add(baseMapTile);
            }
        }

        return walkableMapTiles;
    }

    /// <summary>
    /// Gets the distance to a map tile.
    /// </summary>
    /// <param name="from">From.</param>
    /// <param name="to">To.</param>
    /// <returns></returns>
    public int GetDistanceToCoordinate(Vector2 from, Vector2 to)
    {
        Vector2 directionalVectorToMapTile = from - to;

        return (int) (Mathf.Abs(directionalVectorToMapTile.x) + Mathf.Abs(directionalVectorToMapTile.y));
    }

    /// <summary>
    /// Gets the best way from a unit to a maptile.
    /// The best way is shortest walkable way to the maptile.
    /// Used the A* pathfinding algorithm to find the best path.
    /// </summary>
    /// <param name="unitToMove">The unit to move.</param>
    /// <param name="destinationMapTile">The destination map tile.</param>
    /// <param name="pathfindingNodeDebugData">The pathfinding node debug data.</param>
    /// <returns></returns>
    public List<Vector2> GetBestWayToDestination(BaseUnit unitToMove, BaseMapTile destinationMapTile, out Dictionary<Vector2, PathfindingNodeDebugData> pathfindingNodeDebugData)
    {
        Vector2 startNode = unitToMove.CurrentSimplifiedPosition;
        Vector2 destinationNode = destinationMapTile.SimplifiedMapPosition;
        SimpleUnitBalancing.UnitBalancing unitBalancing = unitToMove.GetUnitBalancing();

#if UNITY_EDITOR
        pathfindingNodeDebugData = new Dictionary<Vector2, PathfindingNodeDebugData>();
#else
        pathfindingNodeDebugData = null;
#endif

        PriorityQueue<Vector2> queueOfNodesToCheck = new PriorityQueue<Vector2>();
        queueOfNodesToCheck.Enqueue(startNode, 0);

        // node; from
        Dictionary<Vector2, Vector2> routeMapping = new Dictionary<Vector2, Vector2>();
        // Also stores which nodes were already checked, entries are updated when a better way to a node was found.
        Dictionary<Vector2, int> costToMoveToNodes = new Dictionary<Vector2, int>();

        while (!queueOfNodesToCheck.IsEmpty)
        {
            Vector2 nodeToGetNeighboursFrom = queueOfNodesToCheck.Dequeue();

            if (nodeToGetNeighboursFrom == destinationNode)
            {
                // Destination found!
                break;
            }

            int costToGetToPreviousMapTile = 0;
            costToMoveToNodes.TryGetValue(nodeToGetNeighboursFrom, out costToGetToPreviousMapTile);

            List<Vector2> adjacentNodes = GetWalkableAdjacentNodes(nodeToGetNeighboursFrom, unitBalancing, costToGetToPreviousMapTile);

            for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
            {
                Vector2 nodeToCheck = adjacentNodes[nodeIndex];
                BaseMapTile baseMapTile;

                if (!m_mapTilePositions.TryGetValue(nodeToCheck, out baseMapTile))
                {
                    Debug.LogErrorFormat("BaseMapTile on position: '{0}' was not found!", nodeToCheck);
                    continue;
                }

                int costToMoveToNode = costToGetToPreviousMapTile +
                                       unitBalancing.GetMovementCostToWalkOnMapTileType(baseMapTile.MapTileType);

                int existingCostToMoveToNode = 0;

                // The node priority describes how good it is to consider taking the note into the best path to the destination.
                // The lower the value the better.
                int nodePriority = costToMoveToNode + GetDistanceToCoordinate(nodeToCheck, destinationNode);

                // Only add node, if it wasn't added before or if a shorter path to the node was found.
                if (!costToMoveToNodes.ContainsKey(nodeToCheck))
                {
                    routeMapping.Add(nodeToCheck, nodeToGetNeighboursFrom);
                    costToMoveToNodes.Add(nodeToCheck, costToMoveToNode);

                    queueOfNodesToCheck.Enqueue(nodeToCheck, nodePriority);

#if UNITY_EDITOR
                    pathfindingNodeDebugData.Add(nodeToCheck, new PathfindingNodeDebugData
                    {
                        CostToMoveToNode = costToMoveToNode,
                        NodePriority = nodePriority,

                        PreviousNode = nodeToGetNeighboursFrom
                    });
#endif
                }
                else if(costToMoveToNodes.TryGetValue(nodeToCheck, out existingCostToMoveToNode) &&
                    costToMoveToNode < existingCostToMoveToNode)
                {
                    routeMapping[nodeToCheck] = nodeToGetNeighboursFrom;
                    costToMoveToNodes[nodeToCheck] = costToMoveToNode;

                    queueOfNodesToCheck.Enqueue(nodeToCheck, nodePriority);

#if UNITY_EDITOR
                    pathfindingNodeDebugData[nodeToCheck] = new PathfindingNodeDebugData
                    {
                        CostToMoveToNode = costToMoveToNode,
                        NodePriority = nodePriority,

                        PreviousNode = nodeToGetNeighboursFrom
                    };
#endif
                }
            }
        }

        return GetRouteListFromRouteMapping(startNode, destinationNode, routeMapping);
    }

    /// <summary>
    /// Gets a route list from a route mapping dictionary.
    /// </summary>
    /// <param name="start">The start.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="routeMapping">The route mapping.</param>
    /// <returns>The return value is the route list or an empty list. So you can always safely get the count of the returned list.</returns>
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
    /// Gets the adjacent tiles of a sourceToGetNeighboursFrom tile position.
    /// This method considers only the previously registered maptiles.
    /// </summary>
    /// <param name="sourceToGetNeighboursFrom">The sourceToGetNeighboursFrom.</param>
    /// <param name="unitBalancing">The unit balancing.</param>
    /// <param name="walkingCostToNode">The already walked map tiles.</param>
    /// <returns></returns>
    private List<Vector2> GetWalkableAdjacentNodes(Vector2 sourceToGetNeighboursFrom, SimpleUnitBalancing.UnitBalancing unitBalancing, 
        int walkingCostToNode)
    {
        List<Vector2> walkableAdjacentNodes = new List<Vector2>();

        List<Vector2> adjacentNodes = GetAdjacentNodes(sourceToGetNeighboursFrom);

        for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
        {
            Vector2 adjacentTile = adjacentNodes[nodeIndex];

            BaseMapTile adjacenBaseMapTile;

            // Is tile position registered?
            if (m_mapTilePositions.TryGetValue(adjacentTile, out adjacenBaseMapTile) &&
                // Is MapTile walkable by unit?
                unitBalancing.CanUnitWalkOnMapTileType(adjacenBaseMapTile.MapTileType) &&
                // Is there a unit on the positon? (rendering it unwalkable)
                !ControllerContainer.BattleController.IsUnitOnNode(adjacentTile) &&
                // Can unit walk on the node, base on the movement range of the unit.
                walkingCostToNode + unitBalancing.GetMovementCostToWalkOnMapTileType(adjacenBaseMapTile.MapTileType) <= unitBalancing.m_MovementRangePerRound)
            {
                walkableAdjacentNodes.Add(adjacentTile);
            }
        }

        return walkableAdjacentNodes;
    }

    /// <summary>
    /// Gets the adjacent nodes.
    /// </summary>
    /// <param name="sourceNode">The source node.</param>
    /// <returns></returns>
    public List<Vector2> GetAdjacentNodes(Vector2 sourceNode)
    {
        List<Vector2> adjacentNodes = new List<Vector2>(4);

        Vector2[] adjacentModifier =
        {
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(-1, 0),
            new Vector2(0, -1)
        };

        for (int modifierIndex = 0; modifierIndex < adjacentModifier.Length; modifierIndex++)
        {
            adjacentNodes.Add(sourceNode + adjacentModifier[modifierIndex]);
        }

        return adjacentNodes;
    }

    /// <summary>
    /// Returns the route marker definitions for a route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <returns></returns>
    public List<KeyValuePair<Vector2, RouteMarkerDefinition>> GetRouteMarkerDefinitions(List<Vector2> route)
    {
        var routeMarkerDefinitions = new List<KeyValuePair<Vector2, RouteMarkerDefinition>>(route.Count);

        // Starting with an index of 1 here, because the node at index 0 is the node the unit is standing on.
        for (int nodeIndex = 1; nodeIndex < route.Count; nodeIndex++)
        {
            RouteMarkerDefinition routeMarkerDefinition = new RouteMarkerDefinition();

            Vector2 nodeToGetRouteMarkerFor = route[nodeIndex];
            Vector2 previousNode = route[nodeIndex - 1];
            Vector2 nextNode = Vector2.zero;
            bool isDestinationNode = false;

            if (nodeIndex < route.Count - 1)
            {
                nextNode = route[nodeIndex + 1];
            }
            else
            {
                isDestinationNode = true;
            }

            Vector2 diffToPreviousNode = previousNode - nodeToGetRouteMarkerFor;
            Vector2 diffToNextNode = !isDestinationNode ? nodeToGetRouteMarkerFor - nextNode : Vector2.zero;
            routeMarkerDefinition.RouteMarkerType = GetRouteMarkerType(diffToPreviousNode, diffToNextNode);
            routeMarkerDefinition.Rotation = GetRouteMarkerRotation(diffToPreviousNode, diffToNextNode, routeMarkerDefinition.RouteMarkerType);

            // Calculate route marker definitions here.
            routeMarkerDefinitions.Add(new KeyValuePair<Vector2, RouteMarkerDefinition>(
                route[nodeIndex], routeMarkerDefinition));
        }

        return routeMarkerDefinitions;
    }

    /// <summary>
    /// Gets the route marker rotation.
    /// </summary>
    /// <param name="diffToPreviousNode">The difference to previous node.</param>
    /// <param name="diffToNextNode">The difference to next node.</param>
    /// <param name="routeMarkerType">Type of the route marker.</param>
    /// <returns></returns>
    private Vector3 GetRouteMarkerRotation(Vector2 diffToPreviousNode, Vector2 diffToNextNode, RouteMarkerType routeMarkerType)
    {
        Vector3 rotation = Vector3.zero;

        CardinalDirection comingFromDirection = GetCardinalDirectionFromNodePositionDiff(diffToPreviousNode, true);
        CardinalDirection goingToDirection = GetCardinalDirectionFromNodePositionDiff(diffToNextNode, false);

        if (routeMarkerType == RouteMarkerType.Destination)
        {
            switch (comingFromDirection)
            {
                case CardinalDirection.North:
                    rotation.y = 90f;
                    break;
                case CardinalDirection.East:
                    rotation.y = 180f;
                    break;
                case CardinalDirection.South:
                    rotation.y = 270f;
                    break;
                case CardinalDirection.West:
                    rotation.y = 0f;
                    break;
            }
        }
        else if (routeMarkerType == RouteMarkerType.Straight)
        {
            if ((comingFromDirection == CardinalDirection.East || comingFromDirection == CardinalDirection.West) &&
                (goingToDirection == CardinalDirection.East || goingToDirection == CardinalDirection.West))
            {
                rotation.y = 0f;
            }
            else
            {
                rotation.y = 90f;
            }
        }
        else if(routeMarkerType == RouteMarkerType.Turn)
        {
            //Debug.LogFormat("Coming from: '{0}' Going to: '{1}'", comingFromDirection, goingToDirection);

            if ((comingFromDirection == CardinalDirection.East && goingToDirection == CardinalDirection.North) ||
                (comingFromDirection == CardinalDirection.North && goingToDirection == CardinalDirection.East))
            {
                rotation.y = 180f;
            }
            else if ((comingFromDirection == CardinalDirection.East && goingToDirection == CardinalDirection.South) ||
                (comingFromDirection == CardinalDirection.South && goingToDirection == CardinalDirection.East))
            {
                rotation.y = 270f;
            }
            else if ((comingFromDirection == CardinalDirection.South && goingToDirection == CardinalDirection.West) ||
                (comingFromDirection == CardinalDirection.West && goingToDirection == CardinalDirection.South))
            {
                rotation.y = 0f;
            }
            else if ((comingFromDirection == CardinalDirection.North && goingToDirection == CardinalDirection.West) || 
                (comingFromDirection == CardinalDirection.West && goingToDirection == CardinalDirection.North))
            {
                rotation.y = 90f;
            }
        }

        return rotation;
    }

    /// <summary>
    /// Gets the cardinal direction from node posittion difference.
    /// </summary>
    /// <param name="nodePositionDiff">The node position difference.</param>
    /// <param name="comingFrom">if set to <c>true</c> [previous node].</param>
    /// <returns></returns>
    public CardinalDirection GetCardinalDirectionFromNodePositionDiff(Vector2 nodePositionDiff, bool comingFrom)
    {
        CardinalDirection direction;

        if (Mathf.Abs(nodePositionDiff.x) > 0)
        {
            if (comingFrom)
            {
                direction = nodePositionDiff.x < 0 ? CardinalDirection.West : CardinalDirection.East;
            }
            else
            {
                direction = nodePositionDiff.x > 0 ? CardinalDirection.West : CardinalDirection.East;
            }
        }
        else
        {
            if (comingFrom)
            {
                direction = nodePositionDiff.y > 0 ? CardinalDirection.North : CardinalDirection.South;
            }
            else
            {
                direction = nodePositionDiff.y < 0 ? CardinalDirection.North : CardinalDirection.South;
            }
        }

        return direction;
    }

    /// <summary>
    /// Gets the type of the route marker.
    /// </summary>
    /// <param name="diffToPreviousNode">The difference to previous node.</param>
    /// <param name="diffToNextNode">The difference to next node.</param>
    /// <returns></returns>
    private RouteMarkerType GetRouteMarkerType(Vector2 diffToPreviousNode, Vector2 diffToNextNode)
    {
        RouteMarkerType routeMarkerType;

        Vector2 diffOfSorroundingNodes = new Vector2(Mathf.Abs(diffToPreviousNode.x) + Mathf.Abs(diffToNextNode.x), Mathf.Abs(diffToPreviousNode.y) + Mathf.Abs(diffToNextNode.y));

        if (diffToNextNode == Vector2.zero)
        {
            routeMarkerType = RouteMarkerType.Destination;
        }
        else if (Mathf.RoundToInt(diffOfSorroundingNodes.x) == 2 || Mathf.RoundToInt(diffOfSorroundingNodes.y) == 2)
        {
            routeMarkerType = RouteMarkerType.Straight;
        }
        else
        {
            routeMarkerType = RouteMarkerType.Turn;
        }

        return routeMarkerType;
    }
}
