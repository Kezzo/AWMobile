using UnityEngine;

namespace AWM.Models
{
    /// <summary>
    /// Model to store data for a maptile border data in one instance.
    /// </summary>
    public class MapTileBorderPrefabData
    {
        public MapTileBorderPrefabData(GameObject prefab)
        {
            Prefab = prefab;
        }

        public GameObject Prefab { get; private set; }

        public Vector3 Position { get; set; }

        public Vector3 Rotation { get; set; }
    }
}
