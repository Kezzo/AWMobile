using System.Collections.Generic;
using UnityEngine;

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
        ControllerContainer.TileNavigationController.Initialize(mapGenerationData.m_LevelSize);
        Vector2 groupsToGenerate = new Vector2(mapGenerationData.m_LevelSize.x / mapGenerationData.m_MapTileGroupSize, 
            mapGenerationData.m_LevelSize.y / mapGenerationData.m_MapTileGroupSize);

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
                rowRoot.name = string.Format("MapTileGroup {0}x{1}",xGroup, zGroup);

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

                            ControllerContainer.TileNavigationController.RegisterMapTile(simplifiedMapTilePosition, baseMapTile);
                            baseMapTile.Initialize(ref mapTile, simplifiedMapTilePosition);
                        }

                        //Debug.LogFormat(levelTile, "Generated MapTile at simplified coordinate: '{0}'", 
                        //    mapGenerationData.GetSimplifiedMapTilePosition(mapTile));
                    }
                }
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
            Vector2 diffToFirstAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[0].SimplifiedMapPosition;
            Vector2 diffToSecondAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[1].SimplifiedMapPosition;

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
                nodePositionDiff = simplifiedMapPosition - adjacentNodes.Find(
                    node => !adjacentAttackableTiles.Exists(tile => tile.SimplifiedMapPosition == node));
                break;
            case AreaTileType.TwoBordersCorner:
                return GetTwoBorderCornerRotation(simplifiedMapPosition,
                    adjacentAttackableTiles, attackRangeCenterPosition);
            case AreaTileType.TwoBorderStraight:

                break;
            case AreaTileType.ThreeBorders:
                // There is only one adjacent attackable tile
                nodePositionDiff = simplifiedMapPosition - adjacentAttackableTiles[0].SimplifiedMapPosition;
                break;
        }

        return ControllerContainer.TileNavigationController.GetRotationFromCardinalDirection(
            ControllerContainer.TileNavigationController.GetCardinalDirectionFromNodePositionDiff(nodePositionDiff));
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

        Vector2 diffToFirstAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[0].SimplifiedMapPosition;
        Vector2 diffToSecondAdjacentTile = simplifiedMapPosition - adjacentAttackableTiles[1].SimplifiedMapPosition;

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
    /// <param name="mapData">The map data that holds the type of each maptile at a specific position. 
    /// This is used to find the maptiles on the adjacent positions and get their MapTileType.</param>
    /// <param name="adjacentWaterDirections">A list containing all cardinal directions the given MapTileType is adjacent to the given tile position.</param>
    /// <returns></returns>
    public bool IsMapTileNextToType(MapTileType mapTileType, Vector2 tilePosition, MapGenerationData mapData, 
        out List<CardinalDirection> adjacentWaterDirections)
    {
        bool isNextToWater = false;
        List<Vector2> adjacentNodes = ControllerContainer.TileNavigationController.GetAdjacentNodes(
            tilePosition, includeAdjacentCorners: true);

        adjacentWaterDirections = new List<CardinalDirection>();

        for (int i = 0; i < adjacentNodes.Count; i++)
        {
            MapGenerationData.MapTile mapTile = mapData.GetMapTileAtPosition(adjacentNodes[i]);

            if (mapTile != null && mapTile.m_MapTileType == mapTileType)
            {
                isNextToWater = true;

                Vector2 positionDiff = tilePosition - adjacentNodes[i];

                Debug.LogFormat("{0} Position: {1} is water! Diff: {2}", tilePosition, adjacentNodes[i], positionDiff);

                adjacentWaterDirections.Add(ControllerContainer.TileNavigationController.
                    GetCardinalDirectionFromNodePositionDiff(positionDiff));
            }
        }

        return isNextToWater;
    }
}
