using AWM.Models;
using UnityEngine;

namespace AWM.MapTileGeneration
{
    /// <summary>
    /// Classes that implement this interface are able to provide generation data of a maptile at a specific position.
    /// </summary>
    public interface IMapTileProvider
    {
        /// <summary>
        /// Returns the generation data of a map tile at the given position.
        /// </summary>
        MapGenerationData.MapTile GetMapTileAtPosition(Vector2 position);
    }
}
