﻿using System.Collections.Generic;
using AWM.EditorAndDebugOnly;
using AWM.Enums;
using AWM.MapTileGeneration;
using AWM.Models;
using AWM.System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// Class to provide tile navigation algorithms results for units.
    /// This controller also implements the A* pathfinding algorithm.
    /// </summary>
    public class TileNavigationController : IMapTileProvider
    {
        public Dictionary<Vector2, BaseMapTile> MapTilePositions { get; private set; }

        /// <summary>
        /// Initializes the TileNavigationController.
        /// </summary>
        /// <param name="mapSize">Size of the map.</param>
        public void Initialize(Vector2 mapSize)
        {
            MapTilePositions = new Dictionary<Vector2, BaseMapTile>((int) (mapSize.x * mapSize.y));
        }

        /// <summary>
        /// Registers a map tile on a position.
        /// </summary>
        /// <param name="mapTilePosition">The map tile position.</param>
        /// <param name="baseMapTile">The base map tile.</param>
        public void RegisterMapTile(Vector2 mapTilePosition, BaseMapTile baseMapTile)
        {
            if (MapTilePositions.ContainsKey(mapTilePosition))
            {
                Debug.LogErrorFormat("Tried to register a maptile under an already registered coordinate: '{0}'. " +
                                     "Not registering the second MapTile!", mapTilePosition);
            }
            else
            {
                MapTilePositions.Add(mapTilePosition, baseMapTile);
            }
        }

        /// <summary>
        /// Gets the map tile on a given simplified position.
        /// </summary>
        /// <param name="mapTilePosition">The map tile position.</param>
        /// <returns></returns>
        public BaseMapTile GetMapTile(Vector2 mapTilePosition)
        {
            BaseMapTile baseMapTile;

            MapTilePositions.TryGetValue(mapTilePosition, out baseMapTile);

            return baseMapTile;
        }

        /// <summary>
        /// Returns all map tiles in range, ignoring the MapTileType.
        /// </summary>
        /// <param name="sourceNode">The source node.</param>
        /// <param name="range">The distance to include maptile from the source node.</param>
        /// <param name="includeSourceNode">If set to true; the returned list will include the map tile at the given source node position.</param>
        public List<BaseMapTile> GetMapTilesInRange(Vector2 sourceNode, int range, bool includeSourceNode = false)
        {
            List<BaseMapTile> mapTilesInRange = new List<BaseMapTile>();

            Queue<Vector2> nodesToCheck = new Queue<Vector2>();
            nodesToCheck.Enqueue(sourceNode);

            while (nodesToCheck.Count > 0)
            {
                Vector2 nodeToCheck = nodesToCheck.Dequeue();

                List<Vector2> adjacentNodes = GetAdjacentNodes(nodeToCheck);

                for (int i = 0; i < adjacentNodes.Count; i++)
                {
                    if (GetDistanceToCoordinate(sourceNode, adjacentNodes[i]) <= range && (includeSourceNode || adjacentNodes[i] != sourceNode))
                    {
                        BaseMapTile mapTileInRange = GetMapTile(adjacentNodes[i]);

                        if (mapTileInRange != null && !mapTilesInRange.Contains(mapTileInRange))
                        {
                            mapTilesInRange.Add(mapTileInRange);
                            nodesToCheck.Enqueue(adjacentNodes[i]);
                        }
                    }
                }
            }

            return mapTilesInRange;
        }

        /// <summary>
        /// Returns a list of walkable maptiles.
        /// </summary>
        /// <param name="unitPosition">The unit position.</param>
        /// <param name="movementCostResolver">The movement cost resolver.</param>
        public List<BaseMapTile> GetWalkableMapTiles(Vector2 unitPosition, IMovementCostResolver movementCostResolver)
        {
            Queue<Vector2> nodesToCheck = new Queue<Vector2>();
            nodesToCheck.Enqueue(unitPosition);

            Dictionary<Vector2, int> costToMoveToNodes = new Dictionary<Vector2, int>();

            while (true)
            {
                Vector2 nodeToCheck = nodesToCheck.Dequeue();

                int walkingCostToNode;
                costToMoveToNodes.TryGetValue(nodeToCheck, out walkingCostToNode);

                List<Vector2> adjacentNodes = GetWalkableAdjacentNodes(nodeToCheck, movementCostResolver, walkingCostToNode);

                for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
                {
                    Vector2 adjacentNode = adjacentNodes[nodeIndex];
                    BaseMapTile baseMapTile;

                    if (!MapTilePositions.TryGetValue(adjacentNode, out baseMapTile))
                    {
                        Debug.LogErrorFormat("BaseMapTile on position: '{0}' was not found!", adjacentNode);
                        continue;
                    }

                    int costToMoveToNode = walkingCostToNode +
                                           movementCostResolver.GetMovementCostToWalkOnMapTile(baseMapTile);

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

                if (MapTilePositions.TryGetValue(walkableNode.Key, out baseMapTile) &&
                    movementCostResolver.CanUnitWalkOnMapTile(baseMapTile, 0, false)) // filter tiles the unit can't stand on
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
        /// <param name="startNode">The node to start from.</param>
        /// <param name="destinationNode">The destination node to move to.</param>
        /// <param name="movementCostResolver">Used to determine how cost of movement and movement allowance is calculated.</param>
        /// <param name="pathfindingNodeDebugData">The dictionary the pathfinding node debug data should added to.</param>
        public List<Vector2> GetBestWayToDestination(Vector2 startNode, Vector2 destinationNode, 
            IMovementCostResolver movementCostResolver, Dictionary<Vector2, PathfindingNodeDebugData> pathfindingNodeDebugData = null)
        {
            if (startNode.Equals(destinationNode))
            {
                return new List<Vector2>();
            }

            PriorityQueue<Vector2> queueOfNodesToCheck = new PriorityQueue<Vector2>();
            queueOfNodesToCheck.Enqueue(startNode, 0);

            // node; from
            Dictionary<Vector2, Vector2> routeMapping = new Dictionary<Vector2, Vector2>();
            // Also stores which nodes were already checked, entries are updated when a better way to a node was found.
            Dictionary<Vector2, int> costToMoveToNodes = new Dictionary<Vector2, int>();

            bool destinationWasFound = false;

            while (!queueOfNodesToCheck.IsEmpty)
            {
                Vector2 nodeToGetNeighboursFrom = queueOfNodesToCheck.Dequeue();

                if (nodeToGetNeighboursFrom == destinationNode)
                {
                    // Destination found!
                    destinationWasFound = true;
                    break;
                }

                int costToGetToPreviousMapTile = 0;
                costToMoveToNodes.TryGetValue(nodeToGetNeighboursFrom, out costToGetToPreviousMapTile);

                List<Vector2> adjacentNodes = GetWalkableAdjacentNodes(nodeToGetNeighboursFrom, movementCostResolver, costToGetToPreviousMapTile);
                
                // Needed to prevent loopback pathfinding that makes assumes that the startnode is a valid node to move to.
                adjacentNodes.Remove(startNode);

                for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
                {
                    Vector2 nodeToCheck = adjacentNodes[nodeIndex];
                    BaseMapTile baseMapTile;

                    if (!MapTilePositions.TryGetValue(nodeToCheck, out baseMapTile))
                    {
                        Debug.LogErrorFormat("BaseMapTile on position: '{0}' was not found!", nodeToCheck);
                        continue;
                    }

                    int costToMoveToNode = costToGetToPreviousMapTile +
                                           movementCostResolver.GetMovementCostToWalkOnMapTile(baseMapTile);

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
                        if (pathfindingNodeDebugData != null)
                        {
                            pathfindingNodeDebugData.Add(nodeToCheck, new PathfindingNodeDebugData
                            {
                                CostToMoveToNode = costToMoveToNode,
                                NodePriority = nodePriority,

                                PreviousNode = nodeToGetNeighboursFrom
                            });
                        }
#endif
                    }
                    else if(costToMoveToNodes.TryGetValue(nodeToCheck, out existingCostToMoveToNode) &&
                            costToMoveToNode < existingCostToMoveToNode)
                    {
                        routeMapping[nodeToCheck] = nodeToGetNeighboursFrom;
                        costToMoveToNodes[nodeToCheck] = costToMoveToNode;

                        queueOfNodesToCheck.Enqueue(nodeToCheck, nodePriority);

#if UNITY_EDITOR
                        if (pathfindingNodeDebugData != null)
                        {
                            pathfindingNodeDebugData[nodeToCheck] = new PathfindingNodeDebugData
                            {
                                CostToMoveToNode = costToMoveToNode,
                                NodePriority = nodePriority,

                                PreviousNode = nodeToGetNeighboursFrom
                            };
                        }
#endif
                    }
                }
            }

            if (!destinationWasFound)
            {
                return null;
            }

            return GetRouteListFromRouteMapping(startNode, destinationNode, routeMapping);
        }

        /// <summary>
        /// Returns the walkable route from a given route.
        /// </summary>
        /// <param name="unitToProcessTurnFor">The unit to process turn for.</param>
        /// <param name="movementCostResolver">The movement cost resolver to use to calculate how much moving to a node costs.</param>
        /// <param name="routeToExtractWalkableRangeFrom">The route to extract walkable range from.</param>
        public List<Vector2> ExtractWalkableRangeFromRoute(BaseUnit unitToProcessTurnFor, IMovementCostResolver movementCostResolver,
            List<Vector2> routeToExtractWalkableRangeFrom)
        {
            List<Vector2> routeToMove = new List<Vector2>();

            int usedUpMovementCost = 0;
            foreach (var routeNode in routeToExtractWalkableRangeFrom)
            {
                if (usedUpMovementCost > unitToProcessTurnFor.GetUnitBalancing().m_MovementRangePerRound)
                {
                    break;
                }

                routeToMove.Add(routeNode);

                usedUpMovementCost += movementCostResolver.GetMovementCostToWalkOnMapTile(
                    CC.TNC.GetMapTile(routeNode));
            }

            // Needed to not stop on a unit when the route would move over it.
            for (int i = routeToMove.Count - 1; i >= 0; i--)
            {
                if(movementCostResolver.CanUnitWalkOnMapTile(CC.TNC.GetMapTile(routeToMove[i]), 0))
                {
                    break;
                }

                routeToMove.RemoveAt(i);
            }

            return routeToMove;
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
                else
                {
                    Debug.LogError("Unable to GetRouteListFromRouteMapping");
                    break;
                }
            }

            bestWayToDestination.Reverse();

            return bestWayToDestination;
        }

        /// <summary>
        /// Gets the adjacent tiles of a sourceToGetNeighborsFrom tile position.
        /// This method considers only the previously registered maptiles.
        /// </summary>
        /// <param name="sourceToGetNeighborsFrom">The sourceToGetNeighborsFrom.</param>
        /// <param name="movementCostResolver">Used to determine how cost of movement and movement allowance is calculated.</param>
        /// <param name="currentWalkingCost">The already walked map tiles.</param>
        /// <returns></returns>
        private List<Vector2> GetWalkableAdjacentNodes(Vector2 sourceToGetNeighborsFrom, IMovementCostResolver movementCostResolver, 
            int currentWalkingCost)
        {
            List<Vector2> walkableAdjacentNodes = new List<Vector2>();

            List<Vector2> adjacentNodes = GetAdjacentNodes(sourceToGetNeighborsFrom);

            for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
            {
                Vector2 adjacentTile = adjacentNodes[nodeIndex];

                BaseMapTile adjacentBaseMapTile;

                // Is tile position registered?
                if (!MapTilePositions.TryGetValue(adjacentTile, out adjacentBaseMapTile))
                {
                    continue;
                }

                int walkingCostToNode = currentWalkingCost + movementCostResolver.GetMovementCostToWalkOnMapTile(adjacentBaseMapTile);

                // Is MapTile walkable by unit?
                if (movementCostResolver.CanUnitWalkOnMapTile(adjacentBaseMapTile, walkingCostToNode, true) &&
                    // Can unit walk on the node, base on the movement range of the unit.
                    movementCostResolver.HasUnitEnoughMovementRangeLeft(walkingCostToNode))
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
        /// <param name="includeAdjacentCorners">Includes the adjacent corners of the given <see cref="sourceNode"/>.</param>
        /// <returns></returns>
        public List<Vector2> GetAdjacentNodes(Vector2 sourceNode, bool includeAdjacentCorners = false)
        {
            List<Vector2> adjacentNodes = new List<Vector2>(includeAdjacentCorners ? 8 : 4);

            Vector2[] adjacentModifier = new Vector2[includeAdjacentCorners ? 8 : 4];

            adjacentModifier[0] = new Vector2(1, 0);
            adjacentModifier[1] = new Vector2(0, 1);
            adjacentModifier[2] = new Vector2(-1, 0);
            adjacentModifier[3] = new Vector2(0, -1);

            if (includeAdjacentCorners)
            {
                adjacentModifier[4] = new Vector2(1, 1);
                adjacentModifier[5] = new Vector2(1, -1);
                adjacentModifier[6] = new Vector2(-1, 1);
                adjacentModifier[7] = new Vector2(-1, -1);
            }

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
            var routeMarkerDefinitions = new List<KeyValuePair<Vector2, RouteMarkerDefinition>>(route != null ? route.Count : 0);

            if (route == null)
            {
                return routeMarkerDefinitions;
            }

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

                List<Vector2> diffToNeighborNodes = new List<Vector2>
                {
                    previousNode - nodeToGetRouteMarkerFor
                };

                if (!isDestinationNode)
                {
                    diffToNeighborNodes.Add(nextNode - nodeToGetRouteMarkerFor);
                }

                routeMarkerDefinition.RouteMarkerType = GetRouteMarkerType(diffToNeighborNodes);
                routeMarkerDefinition.Rotation = GetRouteMarkerRotation(diffToNeighborNodes, routeMarkerDefinition.RouteMarkerType);

                // Calculate route marker definitions here.
                routeMarkerDefinitions.Add(new KeyValuePair<Vector2, RouteMarkerDefinition>(
                    route[nodeIndex], routeMarkerDefinition));
            }

            return routeMarkerDefinitions;
        }

        /// <summary>
        /// Gets the route marker rotation.
        /// </summary>
        /// <param name="diffToNeighborNodes">Contains the position diffs of neighbor nodes used to define the rotation.</param>
        /// <param name="routeMarkerType">Type of the route marker.</param>
        /// <returns></returns>
        public Vector3 GetRouteMarkerRotation(List<Vector2> diffToNeighborNodes, RouteMarkerType routeMarkerType)
        {
            Vector3 rotation = Vector3.zero;

            CardinalDirection comingFromDirection = GetCardinalDirectionFromNodePositionDiff(diffToNeighborNodes.Count > 0 ? diffToNeighborNodes[0] : Vector2.zero);
            CardinalDirection goingToDirection = GetCardinalDirectionFromNodePositionDiff(diffToNeighborNodes.Count > 1 ? diffToNeighborNodes[1] : Vector2.zero);

            switch (routeMarkerType)
            {
                case RouteMarkerType.Destination:
                    rotation.y = GetRotationFromCardinalDirection(comingFromDirection);
                    break;
                case RouteMarkerType.Straight:
                    if ((comingFromDirection == CardinalDirection.East || comingFromDirection == CardinalDirection.West) &&
                        (goingToDirection == CardinalDirection.East || goingToDirection == CardinalDirection.West))
                    {
                        rotation.y = 0f;
                    }
                    else
                    {
                        rotation.y = 90f;
                    }
                    break;
                case RouteMarkerType.Turn:
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
                    break;
                case RouteMarkerType.TriCorner:

                    Vector2 summarizedDiffs = new Vector2();

                    for (int i = 0; i < diffToNeighborNodes.Count; i++)
                    {
                        summarizedDiffs += new Vector2(Mathf.Abs(diffToNeighborNodes[i].x), Mathf.Abs(diffToNeighborNodes[i].y));
                    }
                    
                    Vector2 singularDiff = summarizedDiffs.x > 1 ? 
                        diffToNeighborNodes.Find(diff => Mathf.Abs(diff.y) > 0) : diffToNeighborNodes.Find(diff => Mathf.Abs(diff.x) > 0);

                    rotation.y = GetRotationFromCardinalDirection(GetCardinalDirectionFromNodePositionDiff(singularDiff));

                    break;
                // No case for Crossroads needed since it's rotation doesn't matter.
            }

            return rotation;
        }

        /// <summary>
        /// Returns the rotation from a cardinal direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        public float GetRotationFromCardinalDirection(CardinalDirection direction)
        {
            float rotation = 0f;

            switch (direction)
            {
                case CardinalDirection.North:
                    rotation = 90f;
                    break;
                case CardinalDirection.East:
                    rotation = 180f;
                    break;
                case CardinalDirection.South:
                    rotation = 270f;
                    break;
                case CardinalDirection.West:
                    rotation = 0f;
                    break;
            }

            return rotation;
        }

        /// <summary>
        /// Gets the cardinal direction from node position difference.
        /// </summary>
        /// <param name="nodePositionDiff">The node position difference.</param>
        public CardinalDirection GetCardinalDirectionFromNodePositionDiff(Vector2 nodePositionDiff)
        {
            CardinalDirection direction;

            if (Mathf.Abs(nodePositionDiff.x) > 0 && Mathf.Abs(nodePositionDiff.y) > 0)
            {
                direction = GetIntermediateDirection(nodePositionDiff);
            }
            else
            {
                if (Mathf.Abs(nodePositionDiff.x) > 0)
                {
                    direction = nodePositionDiff.x < 0 ? CardinalDirection.West : CardinalDirection.East;
                }
                else
                {
                    direction = nodePositionDiff.y > 0 ? CardinalDirection.North : CardinalDirection.South;
                }
            }

            return direction;
        }

        /// <summary>
        /// Returns the intermediate direction based on a given position diff.
        /// </summary>
        /// <param name="nodePositionDiff">The node position difference.</param>
        private CardinalDirection GetIntermediateDirection(Vector2 nodePositionDiff)
        {
            CardinalDirection intermediateDirection;

            if (nodePositionDiff.x > 0 && nodePositionDiff.y > 0)
            {
                intermediateDirection = CardinalDirection.NorthEast;
            }
            else if(nodePositionDiff.x > 0 && nodePositionDiff.y < 0)
            {
                intermediateDirection = CardinalDirection.SouthEast;
            }
            else if (nodePositionDiff.x < 0 && nodePositionDiff.y < 0)
            {
                intermediateDirection = CardinalDirection.SouthWest;
            }
            else
            {
                intermediateDirection = CardinalDirection.NorthWest;
            }

            return intermediateDirection;
        }

        /// <summary>
        /// Gets the type of the route marker.
        /// </summary>
        /// <param name="diffToNeighborNodes">Contains the position diffs of neighbor nodes used to define the route marker type.</param>
        public RouteMarkerType GetRouteMarkerType(List<Vector2> diffToNeighborNodes)
        {
            RouteMarkerType routeMarkerType = RouteMarkerType.Destination;

            if (diffToNeighborNodes != null && diffToNeighborNodes.Count > 0)
            {
                switch (diffToNeighborNodes.Count)
                {
                    case 1:
                        routeMarkerType = RouteMarkerType.Destination;
                        break;
                    case 2:
                        Vector2 diffOfSurroundingNodes = new Vector2(Mathf.Abs(diffToNeighborNodes[0].x) + Mathf.Abs(diffToNeighborNodes[1].x), 
                            Mathf.Abs(diffToNeighborNodes[0].y) + Mathf.Abs(diffToNeighborNodes[1].y));

                        if (Mathf.RoundToInt(diffOfSurroundingNodes.x) == 2 || Mathf.RoundToInt(diffOfSurroundingNodes.y) == 2)
                        {
                            routeMarkerType = RouteMarkerType.Straight;
                        }
                        else
                        {
                            routeMarkerType = RouteMarkerType.Turn;
                        }
                        break;
                    case 3:
                        routeMarkerType = RouteMarkerType.TriCorner;
                        break;
                    case 4:
                        routeMarkerType = RouteMarkerType.Crossroads;
                        break;
                }
            }

            return routeMarkerType;
        }

        /// <summary>
        /// Returns the generation data of a map tile at the given position.
        /// </summary>
        public MapGenerationData.MapTile GetMapTileAtPosition(Vector2 position)
        {
            MapGenerationData.MapTile mapTileData = null;

            BaseMapTile baseMapTile;
            if(MapTilePositions.TryGetValue(position, out baseMapTile))
            {
                mapTileData = baseMapTile.MapTileData;
            }

            return mapTileData;
        }
    }
}
