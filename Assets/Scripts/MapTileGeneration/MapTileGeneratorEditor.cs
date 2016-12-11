using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class to generation a map from maptiles in the editor.
/// </summary>
public class MapTileGeneratorEditor : MonoBehaviour
{
    [SerializeField]
    private Vector2 m_levelSize;

    [SerializeField]
    private float m_tileMargin;

    [SerializeField]
    private GameObject m_tilePrefab;

    [SerializeField]
    private Transform m_levelRoot;

    /// <summary>
    /// Generates the map.
    /// </summary>
    public void GenerateMap()
    {
        ClearMap();

        ControllerContainer.MapTileGenerator.GenerateGroups(m_levelSize, m_tileMargin, m_tilePrefab, m_levelRoot);
    }

    /// <summary>
    /// Clears the previous generation.
    /// </summary>
    public void ClearMap()
    {
        List<GameObject> gameObjectsToKill = new List<GameObject>();

        foreach (Transform child in m_levelRoot)
        {
            gameObjectsToKill.Add(child.gameObject);
        }

        for (int i = 0; i < gameObjectsToKill.Count; i++)
        {
            DestroyImmediate(gameObjectsToKill[i]);
        }
    }
}
