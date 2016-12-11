using UnityEngine;

/// <summary>
/// Class that allows the modification of the maptiletype of all maptiles in this group.
/// </summary>
public class BaseMapTileGroup : MonoBehaviour
{
    [SerializeField]
    private MapTileType m_mapTileType;

    /// <summary>
    /// Validates the specified map tile type of all maptile children.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    public void Validate()
    {
        var baseMapTiles = transform.GetComponentsInChildren<BaseMapTile>();

        foreach (var baseMapTile in baseMapTiles)
        {
            baseMapTile.MapTileType = m_mapTileType;
            baseMapTile.Validate();
        }
    }
}
