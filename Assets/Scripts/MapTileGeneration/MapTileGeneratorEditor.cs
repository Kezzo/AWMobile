#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class to generation a map from maptiles in the editor.
/// </summary>
public class MapTileGeneratorEditor : MonoBehaviour
{
    [SerializeField]
    private string m_levelToEdit;

    [SerializeField]
    private Vector2 m_levelSize;

    [SerializeField]
    private float m_tileMargin;

    [SerializeField]
    private GameObject m_tilePrefab;

    [SerializeField]
    private Transform m_levelRoot;

    [SerializeField]
    private GameObject m_baseUnitPrefab;
    public GameObject BaseUnitPrefab { get { return m_baseUnitPrefab; } }

#pragma warning disable 649

    [Serializable]
    private class MapTileTypeAssignment
    {
        public MapTileType m_MapTileType;
        public GameObject m_MapTilePrefab;

        public List<MapTileBorderAssignment> m_MapTileBorders;
    }

    [Serializable]
    public class MapTileBorderAssignment
    {
        public MapTileBorderType m_BorderType;
        public GameObject m_BorderPrefab;
    }

    [SerializeField]
    private List<MapTileTypeAssignment> m_mapTileTypeAssignmentList;

    [Serializable]
    public class UnitTypeAssignment
    {
        public UnitType m_UnitType;
        public Mesh m_UnitMesh;

        public List<TeamMaterialAssignment> m_ColorUvCoordinateAssignments;

        [Serializable]
        public class TeamMaterialAssignment
        {
            public TeamColor m_TeamColor;
            public Material m_Material;
        }
    }

    [SerializeField]
    private List<UnitTypeAssignment> m_unitTypeAssignmentList;

#pragma warning restore 649

    private MapGenerationData m_currentlyVisibleMap;
    public MapGenerationData CurrentlyVisibleMap { get { return m_currentlyVisibleMap; } }

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);
    }

    /// <summary>
    /// Generates the map.
    /// </summary>
    public void GenerateMap()
    {
        ClearMap();

        m_currentlyVisibleMap = ControllerContainer.MapTileGenerationService.GenerateMapGroups(m_levelSize, m_tileMargin, 2);

        ControllerContainer.MapTileGenerationService.LoadGeneratedMap(m_currentlyVisibleMap, m_tilePrefab, m_levelRoot);
    }

    /// <summary>
    /// Loads an existing map.
    /// </summary>
    public void LoadExistingMap(string levelNameToLoad = "")
    {
        string levelToLoad = Application.isPlaying ? levelNameToLoad : m_levelToEdit;

        string assetPath = string.Format("Levels/{0}", levelToLoad);

        MapGenerationData mapGenerationData = ControllerContainer.AssetDatabaseService.GetAssetDataAtPath<MapGenerationData>(assetPath);

        if (mapGenerationData == null)
        {
            Debug.LogErrorFormat("There is no existing map with name: '{0}' in the path: '{1}'", levelToLoad, assetPath);
        }
        else
        {
            ClearMap();
            m_currentlyVisibleMap = mapGenerationData;
            ControllerContainer.MapTileGenerationService.LoadGeneratedMap(mapGenerationData, m_tilePrefab, m_levelRoot);
        }
    }

#if UNITY_EDITOR

    /// <summary>
    /// Gets or sets the name of the save map under level.
    /// </summary>
    /// <value>
    /// The name of the save map under level.
    /// </value>
    public void SaveMapUnderLevelName()
    {
        if (m_currentlyVisibleMap == null)
        {
            Debug.LogError("No map is currently generated! Cannot save it!");
            return;
        }

        if (m_levelToEdit == string.Empty)
        {
            Debug.LogError("Please enter a name this map should be saved under!");
            return;
        }

        m_currentlyVisibleMap.m_LevelName = m_levelToEdit;

        string pathToAsset = string.Format("Assets/Resources/Levels/{0}.asset", m_currentlyVisibleMap.m_LevelName);

        AssetDatabase.CreateAsset(m_currentlyVisibleMap, pathToAsset);
    }

#endif

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

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetSceneByName("Battleground"));
        }
#endif
    }

    /// <summary>
    /// Returns a prefab from the MapTileTypeAssignment based on the given MapTileType.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    /// <returns></returns>
    public GameObject GetPrefabOfMapTileType(MapTileType mapTileType)
    {
        MapTileTypeAssignment mapTileTypeAssignment = GetMapTileAssignment(mapTileType);

        return mapTileTypeAssignment == null ? null : mapTileTypeAssignment.m_MapTilePrefab;
    }

    /// <summary>
    /// Returns the serialized <see cref="MapTileTypeAssignment"/> based on the given <see cref="MapTileType"/>.
    /// </summary>
    /// <param name="mapTileType">The <see cref="MapTileType"/> to get the <see cref="MapTileTypeAssignment"/> for.</param>
    private MapTileTypeAssignment GetMapTileAssignment(MapTileType mapTileType)
    {
        return m_mapTileTypeAssignmentList.Find(prefab => prefab.m_MapTileType == mapTileType);
    }

    /// <summary>
    /// Returns the serialized <see cref="MapTileBorderAssignment"/> based on the given <see cref="MapTileType"/> and <see cref="MapTileBorderType"/>.
    /// </summary>
    /// <param name="mapTileType">The <see cref="MapTileType"/> to get the <see cref="MapTileBorderAssignment"/> for.</param>
    /// <param name="mapTileBorderType">The <see cref="mapTileBorderType"/> to get the <see cref="MapTileBorderAssignment"/> for.</param>
    /// <returns></returns>
    public GameObject GetMapTileBorderPrefab(MapTileType mapTileType, MapTileBorderType mapTileBorderType)
    {
        GameObject borderPrefab = null;
        MapTileTypeAssignment mapTileTypeAssignment = GetMapTileAssignment(mapTileType);

        if (mapTileTypeAssignment != null)
        {
            MapTileBorderAssignment borderAssignment = mapTileTypeAssignment.m_MapTileBorders.Find(
                assignment => assignment.m_BorderType == mapTileBorderType);

            if (borderAssignment == null)
            {
                Debug.LogFormat("MapTileBorderPrefab couldn't be found! MapTileType: {0} MapTileBorderType: {1}",
                    mapTileType, mapTileBorderType);
            }
            else
            {
                borderPrefab = borderAssignment.m_BorderPrefab;
            }
        }

        return borderPrefab;
    }

    /// <summary>
    /// Returns a mesh from the UnitTypeAssignment based on the given UnitType.
    /// </summary>
    /// <param name="unitType">Type of the unit.</param>
    /// <returns></returns>
    public Mesh GetMeshOfUnitType(UnitType unitType)
    {
        UnitTypeAssignment unitTypeAssignment = m_unitTypeAssignmentList.Find(prefab => prefab.m_UnitType == unitType);

        return unitTypeAssignment == null ? null : unitTypeAssignment.m_UnitMesh;
    }

    /// <summary>
    /// Gets the uv coordinate mesh depending on the given team color and unit type.
    /// </summary>
    /// <param name="unitType">Type of the unit.</param>
    /// <param name="teamColor">Color of the team.</param>
    /// <returns></returns>
    public Material GetMaterialForTeamColor(UnitType unitType, TeamColor teamColor)
    {
        Material materialToReturn = null;
        UnitTypeAssignment unitTypeAssignment = m_unitTypeAssignmentList.Find(prefab => prefab.m_UnitType == unitType);

        if (unitTypeAssignment != null)
        {
            UnitTypeAssignment.TeamMaterialAssignment teamMaterialAssignment =
                unitTypeAssignment.m_ColorUvCoordinateAssignments.Find(material => material.m_TeamColor == teamColor);

            if (teamMaterialAssignment != null)
            {
                materialToReturn = teamMaterialAssignment.m_Material;
            }
        }

        return materialToReturn;
    }
}
