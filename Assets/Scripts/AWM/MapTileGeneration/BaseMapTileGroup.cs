using AWM.Enums;
using UnityEngine;

namespace AWM.MapTileGeneration
{
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
        public void Validate()
        {
            if (m_mapTileType == MapTileType.Empty)
            {
                return;
            }

            var baseMapTiles = transform.GetComponentsInChildren<BaseMapTile>();

            foreach (var baseMapTile in baseMapTiles)
            {
                baseMapTile.MapTileType = m_mapTileType;
                baseMapTile.ValidateMapTile();
                baseMapTile.ValidateLevelSelector();
            }
        }
    }
}
