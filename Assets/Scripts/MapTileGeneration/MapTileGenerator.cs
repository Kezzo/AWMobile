using UnityEngine;

/// <summary>
/// Class that holds all map creation methods.
/// </summary>
public class MapTileGenerator
{
    private const int MapTileGroupSize = 2;

    /// <summary>
    /// Generates a simple rect map.
    /// </summary>
    /// <param name="sizeOfLevel">The size of level.</param>
    /// <param name="prefab">The prefab.</param>
    /// <param name="root">The root.</param>
    /// <param name="layerToAssign">The layer to assign.</param>
    public void Generate(Vector2 sizeOfLevel, float tileMargin, GameObject prefab, Transform root)
    {
        float mapMaxX = ((sizeOfLevel.x - 1) * tileMargin / 2);

        for (int x = 0; x < sizeOfLevel.x; x++)
        {
            GameObject rowRoot = new GameObject();
            rowRoot.transform.SetParent(root);
            rowRoot.name = "MapTileRow " + x;

            for (int z = 0; z < sizeOfLevel.y; z++)
            {
                var levelTile = GameObject.Instantiate(prefab);

                levelTile.transform.SetParent(rowRoot.transform);
                levelTile.transform.position = new Vector3(x * tileMargin - mapMaxX, 0f, z * tileMargin);

                BaseMapTile baseMapTile = levelTile.GetComponent<BaseMapTile>();

                if(baseMapTile != null)
                {
                    baseMapTile.Initialize();
                }
            }
        }
    }

    /// <summary>
    /// Generates a map sliced into maptile groups.
    /// </summary>
    /// <param name="sizeOfLevel">The size of level.</param>
    /// <param name="tileMargin">The tile margin.</param>
    /// <param name="prefab">The prefab.</param>
    /// <param name="root">The root.</param>
    public void GenerateGroups(Vector2 sizeOfLevel, float tileMargin, GameObject prefab, Transform root)
    {
        Vector2 groupsToGenerate = new Vector2(sizeOfLevel.x / MapTileGroupSize, sizeOfLevel.y / MapTileGroupSize);

        float mapMaxX = ((sizeOfLevel.x - 1) * tileMargin / 2);

        for (int xGroup = 0; xGroup < groupsToGenerate.x; xGroup++)
        {
            for (int zGroup = 0; zGroup < groupsToGenerate.y; zGroup++)
            {
                GameObject rowRoot = new GameObject();
                rowRoot.AddComponent<BaseMapTileGroup>();
                rowRoot.transform.SetParent(root);
                rowRoot.transform.position = new Vector3(xGroup * MapTileGroupSize * tileMargin - mapMaxX, 0f, zGroup * MapTileGroupSize * tileMargin);
                rowRoot.name = string.Format("MapTileGroup {0}x{1}",xGroup, zGroup);

                for (int x = 0; x < MapTileGroupSize; x++)
                {
                    for (int z = 0; z < MapTileGroupSize; z++)
                    {
                        var levelTile = GameObject.Instantiate(prefab);

                        levelTile.transform.SetParent(rowRoot.transform);
                        levelTile.transform.localPosition = new Vector3(x * tileMargin, 0f, z * tileMargin);

                        BaseMapTile baseMapTile = levelTile.GetComponent<BaseMapTile>();

                        if (baseMapTile != null)
                        {
                            baseMapTile.Initialize();
                        }
                    }
                }
            }
        }
    }
}
