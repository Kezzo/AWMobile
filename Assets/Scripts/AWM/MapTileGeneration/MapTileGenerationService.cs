using System;
using System.Collections.Generic;
using System.ComponentModel;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEngine;
using UnityEngine.Assertions;

namespace AWM.MapTileGeneration
{
    /// <summary>
    /// Class that holds all map creation methods.
    /// </summary>
    public class MapTileGenerationService
    {
        /// <summary>
        /// Generates a map sliced into maptile groups.
        /// </summary>
        /// <param name="mapGenerationData">The map generation data.</param>
        /// <param name="prefab">The prefab.</param>
        /// <param name="root">The root.</param>
        public void LoadGeneratedMap(MapGenerationData mapGenerationData, GameObject prefab, Transform root)
        {
            CC.TNC.Initialize(mapGenerationData.m_LevelSize);
            Vector2 groupsToGenerate = new Vector2(mapGenerationData.m_LevelSize.x / mapGenerationData.m_MapTileGroupSize,
                mapGenerationData.m_LevelSize.y / mapGenerationData.m_MapTileGroupSize);

            List<BaseMapTile> generatedMapTiles = new List<BaseMapTile>((int)(mapGenerationData.m_LevelSize.x * mapGenerationData.m_LevelSize.y));

            for (int xGroup = 0; xGroup < groupsToGenerate.x; xGroup++)
            {
                for (int zGroup = 0; zGroup < groupsToGenerate.y; zGroup++)
                {
                    MapGenerationData.MapTileGroup mapTileGroup =
                        mapGenerationData.GetMapTileGroupAtMapVector(new Vector2(xGroup, zGroup));

                    if (mapTileGroup == null)
                    {
                        Debug.LogErrorFormat("MapTileGroup with map vector: '{0}' not found!", new Vector2(xGroup, zGroup));

                        continue;
                    }

                    GameObject rowRoot = new GameObject();
                    rowRoot.AddComponent<BaseMapTileGroup>();
                    rowRoot.transform.SetParent(root);
                    rowRoot.transform.position = mapTileGroup.m_GroupPosition;
                    rowRoot.name = string.Format("MapTileGroup {0}x{1}", xGroup, zGroup);

                    for (int x = 0; x < mapGenerationData.m_MapTileGroupSize; x++)
                    {
                        for (int z = 0; z < mapGenerationData.m_MapTileGroupSize; z++)
                        {
                            MapGenerationData.MapTile mapTile = mapTileGroup.GetMapTileAtGroupPosition(new Vector2(x, z));

                            var levelTile = GameObject.Instantiate(prefab);

                            levelTile.transform.SetParent(rowRoot.transform);
                            levelTile.transform.localPosition = mapTile.m_LocalPositionInGroup;

                            BaseMapTile baseMapTile = levelTile.GetComponent<BaseMapTile>();

                            if (baseMapTile == null)
                            {
                                Debug.LogError("No BaseMapTile Component was found on the MapTile!", levelTile);
                            }
                            else
                            {
                                Vector2 simplifiedMapTilePosition = mapGenerationData.GetSimplifiedMapTilePosition(mapTile);

                                CC.TNC.RegisterMapTile(simplifiedMapTilePosition, baseMapTile);
                                baseMapTile.Initialize(ref mapTile, simplifiedMapTilePosition);

                                generatedMapTiles.Add(baseMapTile);
                            }

                            //Debug.LogFormat(levelTile, "Generated MapTile at simplified coordinate: '{0}'", 
                            //    mapGenerationData.GetSimplifiedMapTilePosition(mapTile));
                        }
                    }
                }
            }

            for (int i = 0; i < generatedMapTiles.Count; i++)
            {
                if (generatedMapTiles[i].MapTileData.m_HasStreet)
                {
                    generatedMapTiles[i].ValidateStreetTileAddition(true);
                }
            }

            if (mapGenerationData.m_IsLevelSelection)
            {
                CC.LSIC.InitializeLevelSelectionVisuals();
            }

            if (Application.isPlaying)
            {
                CombineMaptileMeshes(root, generatedMapTiles);
                CombineEnvironmentPropMeshes(root, generatedMapTiles);
            }   
        }

        /// <summary>
        /// Will get the meshes of all generated maptiles and combine them.
        /// This is done for performance improvements.
        /// </summary>
        /// <param name="root">The root object under which all maptiles are parented. Will hold the combined mesh.</param>
        /// <param name="generatedMapTiles">All generated map tiles. Needed to access the meshes to combine.</param>
        private void CombineMaptileMeshes(Transform root, List<BaseMapTile> generatedMapTiles)
        {
            List<CombineInstance> combineInstances = new List<CombineInstance>();
            foreach (var generatedMapTile in generatedMapTiles)
            {
                foreach (var meshFilter in generatedMapTile.GetMeshFilters())
                {
                    combineInstances.Add(new CombineInstance
                    {
                        mesh = meshFilter.sharedMesh,
                        transform = meshFilter.transform.localToWorldMatrix
                    });
                }
            }

            root.gameObject.AddComponent<MeshFilter>().mesh = new Mesh();
            root.gameObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combineInstances.ToArray());

            foreach (var generatedMapTile in generatedMapTiles)
            {
                generatedMapTile.RemoveRenderingComponents();
            }
        }

        /// <summary>
        /// Combines the environment prop meshes of the given maptiles.
        /// </summary>
        /// <param name="root">The root to get the root transform from.</param>
        /// <param name="generatedMapTiles">The generated map tiles.</param>
        private void CombineEnvironmentPropMeshes(Transform root, List<BaseMapTile> generatedMapTiles)
        {
            Dictionary<EnvironmentPropType, List<GameObject>> mergeableEnvironmentProps = new Dictionary<EnvironmentPropType, List<GameObject>>();
           
            // Get mergeable environment props per type
            foreach (var generatedMapTile in generatedMapTiles)
            {
                foreach (var environmentInstantiateHelper in generatedMapTile.EnvironmentInstantiateHelper)
                {
                    if (environmentInstantiateHelper == null)
                    {
                        continue;
                    }

                    foreach (var environmentProp in environmentInstantiateHelper.GetMergeablePropsByType())
                    {
                        if (!mergeableEnvironmentProps.ContainsKey(environmentProp.Key))
                        {
                            mergeableEnvironmentProps.Add(environmentProp.Key, new List<GameObject>());
                        }

                        mergeableEnvironmentProps[environmentProp.Key].AddRange(environmentProp.Value);
                    }
                }
            }

            Dictionary<EnvironmentPropType, List<CombineInstance>> combineInstancesByType = new Dictionary<EnvironmentPropType, List<CombineInstance>>();

            MeshFilter meshFilter = null;

            // Create a list of combine instance per environment type
            foreach (var mergeableEnvironmentProp in mergeableEnvironmentProps)
            {
                if (!combineInstancesByType.ContainsKey(mergeableEnvironmentProp.Key))
                {
                    combineInstancesByType.Add(mergeableEnvironmentProp.Key, new List<CombineInstance>());
                }

                foreach (var environmentProp in mergeableEnvironmentProp.Value)
                {
                    meshFilter = environmentProp.GetComponent<MeshFilter>();

                    combineInstancesByType[mergeableEnvironmentProp.Key].Add(new CombineInstance
                    {
                        mesh = meshFilter.sharedMesh,
                        transform = meshFilter.transform.localToWorldMatrix
                    });
                }
            }

            EnvironmentPropRootHelper environmentPropRootHelper = root.GetComponent<EnvironmentPropRootHelper>();

            // Combine meshes of the same environment type.
            foreach (var combineInstances in combineInstancesByType)
            {
                Transform environmentRoot = environmentPropRootHelper.GetEnvironmentPropTypeRoot(combineInstances.Key);

                environmentRoot.gameObject.AddComponent<MeshFilter>().mesh = new Mesh();
                environmentRoot.gameObject.GetComponent<MeshFilter>().mesh.CombineMeshes(combineInstances.Value.ToArray());
            }

            foreach (var mergeableEnvironmentProp in mergeableEnvironmentProps)
            {
                foreach (var gameObject in mergeableEnvironmentProp.Value)
                {
                    environmentPropRootHelper.RemoveRenderingComponents(gameObject);
                }
            }
        }

        /// <summary>
        /// Generates the map groups.
        /// </summary>
        /// <param name="sizeOfLevel">The size of level.</param>
        /// <param name="tileMargin">The tile margin.</param>
        /// <param name="mapTileGroupSize">Size of the map tile group.</param>
        /// <returns></returns>
        public MapGenerationData GenerateMapGroups(Vector2 sizeOfLevel, float tileMargin, int mapTileGroupSize)
        {
            MapGenerationData mapGenerationData = ScriptableObject.CreateInstance<MapGenerationData>();

            mapGenerationData.m_LevelSize = new Vector2(sizeOfLevel.x, sizeOfLevel.y);
            mapGenerationData.m_MapTileMargin = tileMargin;

            mapGenerationData.m_MapTileGroupSize = mapTileGroupSize;
            mapGenerationData.m_MapTileGroups = new List<MapGenerationData.MapTileGroup>((int) (mapGenerationData.m_LevelSize.x * mapGenerationData.m_LevelSize.y));

            // To center the map
            float mapMaxX = ((sizeOfLevel.x - 1) * tileMargin / 2);
            float mapMaxZ = ((sizeOfLevel.y - 1) * tileMargin / 2);

            for (int xGroup = 0; xGroup < mapGenerationData.m_LevelSize.x / mapTileGroupSize; xGroup++)
            {
                for (int zGroup = 0; zGroup < mapGenerationData.m_LevelSize.y / mapTileGroupSize; zGroup++)
                {
                    MapGenerationData.MapTileGroup mapTileGroup = new MapGenerationData.MapTileGroup
                    {
                        m_GroupPositionVector = new Vector2(xGroup, zGroup),
                        m_GroupPosition = new Vector3(xGroup * mapTileGroupSize * tileMargin - mapMaxX, 0f,
                            zGroup * mapTileGroupSize * tileMargin - mapMaxZ),
                        m_MapTiles = new List<MapGenerationData.MapTile>(mapTileGroupSize * mapTileGroupSize)
                    };

                    for (int x = 0; x < mapTileGroupSize; x++)
                    {
                        for (int z = 0; z < mapTileGroupSize; z++)
                        {
                            MapGenerationData.MapTile mapTile = new MapGenerationData.MapTile
                            {
                                m_PositionVector = new Vector2(x, z),
                                m_LocalPositionInGroup = new Vector3(x * tileMargin, 0f, z * tileMargin),
                                m_MapTileType = MapTileType.Water
                            };

                            mapTileGroup.m_MapTiles.Add(mapTile);
                        }
                    }

                    mapGenerationData.m_MapTileGroups.Add(mapTileGroup);
                }
            }

            return mapGenerationData;
        }

        /// <summary>
        /// Returns the either the corner or the straight version of the areatiletype depending on the given adjacent attackable tiles.
        /// </summary>
        /// <param name="simplifiedMapPosition">The simplified positon of the maptile.</param>
        /// <param name="adjacentAttackableTiles">The adjacent attackable tiles to base the selection on.</param>
        public AreaTileType GetTwoBorderAreaTileType(Vector2 simplifiedMapPosition, List<BaseMapTile> adjacentAttackableTiles)
        {
            AreaTileType twoBorderAreaTileType = AreaTileType.NoBorders;

            // We have to assume 2 as a count here, because otherwise this method was called incorrectly.
            if (adjacentAttackableTiles.Count == 2)
            {
                Vector2 diffToFirstAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[0].m_SimplifiedMapPosition;
                Vector2 diffToSecondAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[1].m_SimplifiedMapPosition;

                Vector2 combindedDiff = diffToFirstAdjacentTile - diffToSecondAdjacentTile;

                twoBorderAreaTileType = Mathf.Abs((int)combindedDiff.x) == 1 && Mathf.Abs((int)combindedDiff.y) == 1 ?
                    AreaTileType.TwoBordersCorner :
                    AreaTileType.TwoBorderStraight;
            }

            return twoBorderAreaTileType;
        }

        /// <summary>
        /// Returns the attack marker border rotation.
        /// </summary>
        /// <param name="simplifiedMapPosition">The simplified position of the maptile.</param>
        /// <param name="areaTileType">The areaTileType of the attack marker.</param>
        /// <param name="adjacentNodes">The adjacent nodes.</param>
        /// <param name="adjacentAttackableTiles">The adjacent attackable tiles.</param>
        /// <param name="attackRangeCenterPosition">The center position of the attack range border.</param>
        public float GetAttackMarkerBorderRotation(Vector2 simplifiedMapPosition, AreaTileType areaTileType, List<Vector2> adjacentNodes,
            List<BaseMapTile> adjacentAttackableTiles, Vector2 attackRangeCenterPosition)
        {
            Vector2 nodePositionDiff = Vector2.zero;

            switch (areaTileType)
            {
                case AreaTileType.OneBorder:
                    nodePositionDiff = adjacentNodes.Find(
                        node => !adjacentAttackableTiles.Exists(tile => tile.m_SimplifiedMapPosition == node)) - simplifiedMapPosition;
                    break;
                case AreaTileType.TwoBordersCorner:
                    return GetTwoBorderCornerRotation(simplifiedMapPosition,
                        adjacentAttackableTiles, attackRangeCenterPosition);
                case AreaTileType.TwoBorderStraight:

                    break;
                case AreaTileType.ThreeBorders:
                    // There is only one adjacent attackable tile
                    nodePositionDiff = simplifiedMapPosition - adjacentAttackableTiles[0].m_SimplifiedMapPosition;
                    break;
            }

            return CC.TNC.GetRotationFromCardinalDirection(
                CC.TNC.GetCardinalDirectionFromNodePositionDiff(nodePositionDiff));
        }

        /// <summary>
        /// Returns the attack range marker rotation for two border corner attack range marker.
        /// </summary>
        /// <param name="simplifiedMapPosition">The simplified position of the maptile.</param>
        /// <param name="adjacentAttackableTiles">The adjacent attackable tiles.</param>
        /// <param name="attackRangeCenterPosition">The center position of the attack range border.</param>
        private float GetTwoBorderCornerRotation(Vector2 simplifiedMapPosition, List<BaseMapTile> adjacentAttackableTiles, Vector2 attackRangeCenterPosition)
        {
            float rotationToReturn = 0f;

            Vector2 diffToFirstAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[0].m_SimplifiedMapPosition;
            Vector2 diffToSecondAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[1].m_SimplifiedMapPosition;

            Vector2 combindedDiff = diffToFirstAdjacentTile - diffToSecondAdjacentTile;

            if (combindedDiff.Equals(new Vector2(1, -1)))
            {
                rotationToReturn = 90f;
            }
            else if (combindedDiff.Equals(new Vector2(-1, 1)))
            {
                rotationToReturn = 270f;
            }
            else if (simplifiedMapPosition.x > attackRangeCenterPosition.x)
            {
                rotationToReturn = 180f;
            }

            return rotationToReturn;
        }

        /// <summary>
        /// Checks the adjacent and corner-adjacent tiles of the given tileposition and checks if those tiles have the given MapTileType.
        /// </summary>
        /// <param name="mapTileType">The MapTileType to check for.</param>
        /// <param name="tilePosition">The tile position to get the adjacent tiles from.</param>
        /// <param name="mapTileProvider">Provides the data that holds the type of maptiles at a specific position. 
        /// This is used to find the maptiles on the adjacent positions and get their MapTileType.</param>
        /// <param name="adjacentWaterDirections">A list containing all cardinal directions the given MapTileType is adjacent to the given tile position.</param>
        /// <returns></returns>
        public bool IsMapTileNextToTypes(MapTileType mapTileType, Vector2 tilePosition, IMapTileProvider mapTileProvider, 
            out List<CardinalDirection> adjacentWaterDirections)
        {
            bool isNextToMapTileType = false;
            List<Vector2> adjacentNodes = CC.TNC.GetAdjacentNodes(
                tilePosition, includeAdjacentCorners: true);

            adjacentWaterDirections = new List<CardinalDirection>();

            for (int i = 0; i < adjacentNodes.Count; i++)
            {
                MapGenerationData.MapTile mapTile = mapTileProvider.GetMapTileAtPosition(adjacentNodes[i]);

                if (mapTile != null && mapTile.m_MapTileType == mapTileType)
                {
                    Vector2 positionDiff = tilePosition - adjacentNodes[i];

                    // to consider a bridge as adjacent land or water depending on it's rotation.
                    if (mapTileType == MapTileType.Water && mapTile.m_HasStreet)
                    {
                        MapGenerationData.MapTile originMapTile = mapTileProvider.GetMapTileAtPosition(tilePosition);
                        MapGenerationData.MapTile mapTileBehindAdjacentTile = mapTileProvider.GetMapTileAtPosition(tilePosition + positionDiff*2);

                        if ((originMapTile != null && originMapTile.m_HasStreet) ||
                            (mapTileBehindAdjacentTile != null && mapTileBehindAdjacentTile.m_HasStreet))
                        {
                            continue;
                        }
                    }

                    isNextToMapTileType = true;

                    adjacentWaterDirections.Add(CC.TNC.
                        GetCardinalDirectionFromNodePositionDiff(positionDiff));
                }
            }

            return isNextToMapTileType;
        }

        /// <summary>
        /// Gets border types mapped to cardinal directions to be able to correctly display border to water.
        /// </summary>
        /// <param name="adjacentWaterDirections">The adjacent water directions.</param>
        public List<KeyValuePair<CardinalDirection, MapTileBorderType>> GetBorderDirections(List<CardinalDirection> adjacentWaterDirections)
        {
            var borderDirections = new List<KeyValuePair<CardinalDirection, MapTileBorderType>>();

            for (int i = 0; i < adjacentWaterDirections.Count; i++)
            {
                if (!IsDirectionIntermediate(adjacentWaterDirections[i]))
                {
                    AddNonIntermediateBorder(adjacentWaterDirections, borderDirections, adjacentWaterDirections[i], true);
                    AddNonIntermediateBorder(adjacentWaterDirections, borderDirections, adjacentWaterDirections[i],
                        false);
                }
                else
                {
                    var directionBorderPair = GetDirectionsNextToIntermediateDirection(adjacentWaterDirections[i]);

                    if (!adjacentWaterDirections.Contains(directionBorderPair[0]) && 
                        !adjacentWaterDirections.Contains(directionBorderPair[1]))
                    {
                        borderDirections.Add(new KeyValuePair<CardinalDirection, MapTileBorderType>(
                            adjacentWaterDirections[i], MapTileBorderType.InnerCorner));
                    }
                }
            }
      
            // Fill up empty corner with no border blocks.
            for (int i = borderDirections.Count; i < 4; i++)
            {
                CardinalDirection freeIntermediateDirection;

                if (TryGetFreeIntermediateDirection(
                    adjacentWaterDirections, out freeIntermediateDirection))
                {
                    adjacentWaterDirections.Add(freeIntermediateDirection);
                    borderDirections.Add(new KeyValuePair<CardinalDirection, MapTileBorderType>(
                        freeIntermediateDirection, MapTileBorderType.NoBorder));
                }
            }

            Assert.IsTrue(borderDirections.Count == 4);
            return borderDirections;
        }

        /// <summary>
        /// Adds a non intermediate border to the given border dictionary.
        /// </summary>
        /// <param name="adjacentWaterDirections">The adjacent water directions.</param>
        /// <param name="borderDirections">The border dictionary to add the border to.</param>
        /// <param name="cardinalDirection">The cardinal direction to add the border for.</param>
        /// <param name="checkRightDirection">Determines if the adjacent direction right of the given direction should 
        /// be checked to potentially add a corner border.</param>
        private void AddNonIntermediateBorder(List<CardinalDirection> adjacentWaterDirections,
            List<KeyValuePair<CardinalDirection, MapTileBorderType>> borderDirections, CardinalDirection cardinalDirection, bool checkRightDirection)
        {
            CardinalDirection adjacentDirection;

            // Check if right/left is also water to add a outer corner.
            if (ContainsAdjacentDirection(cardinalDirection, checkRightDirection,
                adjacentWaterDirections, out adjacentDirection))
            {
                CardinalDirection intermediateCardinalDirection = GetIntermediateDirection(
                    cardinalDirection, adjacentDirection);

                var directionBorderPair = new KeyValuePair<CardinalDirection, MapTileBorderType>(
                    intermediateCardinalDirection, MapTileBorderType.OuterCorner);

                if (!borderDirections.Contains(directionBorderPair))
                {
                    borderDirections.Add(directionBorderPair);
                }
            }
            else // if right/left is no water a straight border can be added
            {
                borderDirections.Add(new KeyValuePair<CardinalDirection, MapTileBorderType>(cardinalDirection, checkRightDirection ?
                    MapTileBorderType.StraightRightAligned : MapTileBorderType.StraightLeftAligned));
            }
        }

        /// <summary>
        /// Checks all available directions to find a free intermediate direction. 
        /// It's free the intermediate direction and adjacent directions are not in the list.
        /// I.e. north-east is free, when north-east, north and east is not in the list.
        /// </summary>
        /// <param name="availableDirections">The available directions.</param>
        /// <param name="freeIntermediateDirection">The free intermediate direction.</param>
        /// <returns>Returns true when a free intermediate action was found; false otherwise.</returns>
        private bool TryGetFreeIntermediateDirection(List<CardinalDirection> availableDirections, 
            out CardinalDirection freeIntermediateDirection)
        {
            if (!availableDirections.Contains(CardinalDirection.NorthEast) &&
                !availableDirections.Contains(CardinalDirection.North) &&
                !availableDirections.Contains(CardinalDirection.East))
            {
                freeIntermediateDirection = CardinalDirection.NorthEast;
                return true;
            }

            if (!availableDirections.Contains(CardinalDirection.NorthWest) &&
                !availableDirections.Contains(CardinalDirection.North) &&
                !availableDirections.Contains(CardinalDirection.West))
            {
                freeIntermediateDirection = CardinalDirection.NorthWest;
                return true;
            }

            if (!availableDirections.Contains(CardinalDirection.SouthEast) &&
                !availableDirections.Contains(CardinalDirection.South) &&
                !availableDirections.Contains(CardinalDirection.East))
            {
                freeIntermediateDirection = CardinalDirection.SouthEast;
                return true;
            }

            if (!availableDirections.Contains(CardinalDirection.SouthWest) &&
                !availableDirections.Contains(CardinalDirection.South) &&
                !availableDirections.Contains(CardinalDirection.West))
            {
                freeIntermediateDirection = CardinalDirection.SouthWest;
                return true;
            }

            freeIntermediateDirection = CardinalDirection.East;
            return false;
        }

        /// <summary>
        /// Determines whether a given direction is an intermediate cardinal direction.
        /// </summary>
        /// <param name="direction">The given direction.</param>
        /// <returns>True when it's an intermediate direction; otherwise false.</returns>
        private bool IsDirectionIntermediate(CardinalDirection direction)
        {
            return direction == CardinalDirection.NorthEast ||
                   direction == CardinalDirection.NorthWest ||
                   direction == CardinalDirection.SouthEast ||
                   direction == CardinalDirection.SouthWest;
        }

        /// <summary>
        /// Determines whether the opposite direction of the given action is contained in the given direction list.
        /// </summary>
        /// <param name="direction">The direction to base check on.</param>
        /// <param name="directionsToCheck">The direction list to check.</param>
        /// <returns>Returns true when opposite direction is in the list; otherwise false.</returns>
        private bool ContainsOppositeDirection(CardinalDirection direction, List<CardinalDirection> directionsToCheck)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    return directionsToCheck.Contains(CardinalDirection.South);
                case CardinalDirection.East:
                    return directionsToCheck.Contains(CardinalDirection.West);
                case CardinalDirection.South:
                    return directionsToCheck.Contains(CardinalDirection.North);
                case CardinalDirection.West:
                    return directionsToCheck.Contains(CardinalDirection.East);
            }

            return false;
        }

        /// <summary>
        /// Determines whether an adjacent direction is in a directions list.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="checkRight">If set to <c>true</c> check right of the given direction; 
        /// otherwise the left direction is checked.</param>
        /// <param name="directionsToCheck">The list of directions to check.</param>
        /// <param name="adjacentDirection">The adjacent direction.</param>
        /// <returns>Returns true when the adjacent direction is in the list.</returns>
        private bool ContainsAdjacentDirection(CardinalDirection direction, bool checkRight, 
            List<CardinalDirection> directionsToCheck, out CardinalDirection adjacentDirection)
        {
            switch (direction)
            {
                case CardinalDirection.North:
                    adjacentDirection = checkRight ? CardinalDirection.East : CardinalDirection.West;
                    return directionsToCheck.Contains(adjacentDirection);
                case CardinalDirection.East:
                    adjacentDirection = checkRight ? CardinalDirection.South : CardinalDirection.North;
                    return directionsToCheck.Contains(adjacentDirection);
                case CardinalDirection.South:
                    adjacentDirection = checkRight ? CardinalDirection.West : CardinalDirection.East;
                    return directionsToCheck.Contains(adjacentDirection);
                case CardinalDirection.West:
                    adjacentDirection = checkRight ? CardinalDirection.North : CardinalDirection.South;
                    return directionsToCheck.Contains(adjacentDirection);
                default:
                    throw new ArgumentOutOfRangeException("direction", direction, null);
            }
        }

        /// <summary>
        /// Returns the intermediate cardinal direction based on two given cardinal directions.
        /// </summary>
        /// <param name="firstDirection">The first cardinal direction.</param>
        /// <param name="secondDirection">The second cardinal direction.</param>
        private CardinalDirection GetIntermediateDirection(CardinalDirection firstDirection,
            CardinalDirection secondDirection)
        {
            switch (firstDirection)
            {
                case CardinalDirection.North:
                    switch (secondDirection)
                    {
                        case CardinalDirection.East:
                            return CardinalDirection.NorthEast;
                        case CardinalDirection.West:
                            return CardinalDirection.NorthWest;
                    }
                    break;
                case CardinalDirection.South:
                    switch (secondDirection)
                    {
                        case CardinalDirection.East:
                            return CardinalDirection.SouthEast;
                        case CardinalDirection.West:
                            return CardinalDirection.SouthWest;
                    }
                    break;
                case CardinalDirection.East:
                    switch (secondDirection)
                    {
                        case CardinalDirection.North:
                            return CardinalDirection.NorthEast;
                        case CardinalDirection.South:
                            return CardinalDirection.SouthEast;
                    }
                    break;
                case CardinalDirection.West:
                    switch (secondDirection)
                    {
                        case CardinalDirection.North:
                            return CardinalDirection.NorthWest;
                        case CardinalDirection.South:
                            return CardinalDirection.SouthWest;
                    }
                    break;
            }

            throw new InvalidEnumArgumentException();
        }

        /// <summary>
        /// Returns the directions next to a given intermediate direction.
        /// </summary>
        /// <param name="intermediateDirection">The intermediate direction.</param>
        private CardinalDirection[] GetDirectionsNextToIntermediateDirection(CardinalDirection intermediateDirection)
        {
            var directionsNextToGivenDirection = new CardinalDirection[2];

            switch (intermediateDirection)
            {
                case CardinalDirection.NorthEast:
                    directionsNextToGivenDirection[0] = CardinalDirection.North;
                    directionsNextToGivenDirection[1] = CardinalDirection.East;
                    break;
                case CardinalDirection.NorthWest:
                    directionsNextToGivenDirection[0] = CardinalDirection.North;
                    directionsNextToGivenDirection[1] = CardinalDirection.West;
                    break;
                case CardinalDirection.SouthEast:
                    directionsNextToGivenDirection[0] = CardinalDirection.South;
                    directionsNextToGivenDirection[1] = CardinalDirection.East;
                    break;
                case CardinalDirection.SouthWest:
                    directionsNextToGivenDirection[0] = CardinalDirection.South;
                    directionsNextToGivenDirection[1] = CardinalDirection.West;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("intermediateDirection", intermediateDirection, null);
            }

            return directionsNextToGivenDirection;
        }
    }
}
