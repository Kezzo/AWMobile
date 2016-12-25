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
        Vector2 groupsToGenerate = new Vector2(mapGenerationData.m_LevelSize.x / mapGenerationData.m_MapTileGroupSize, mapGenerationData.m_LevelSize.y / mapGenerationData.m_MapTileGroupSize);

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
                            Vector2 simplifiedMapTilePosition = mapGenerationData.GetSimplifiedMapTileCoordinate(mapTile);

                            ControllerContainer.TileNavigationController.RegisterMapTile(simplifiedMapTilePosition, baseMapTile);
                            baseMapTile.Initialize(ref mapTile, simplifiedMapTilePosition);
                        }

                        //Debug.LogFormat(levelTile, "Generated MapTile at simplified coordinate: '{0}'", 
                        //    mapGenerationData.GetSimplifiedMapTileCoordinate(mapTile));
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
}
