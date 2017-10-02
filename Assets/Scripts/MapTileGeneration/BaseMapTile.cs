﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#pragma warning disable 0649
/// <summary>
/// Represents a parent object of a maptile prefab.
/// Controls which type of maptile is active as a child.
/// </summary>
public class BaseMapTile : MonoBehaviour
{
    [SerializeField]
    private MapTileType m_mapTileType;

    public MapTileType MapTileType
    {
        get { return m_mapTileType; }
        set { m_mapTileType = value; }
    }

    [SerializeField]
    private MapGenerationData.Unit m_unitOnThisTile;

    [SerializeField]
    private Transform m_unitRoot;
    public Transform UnitRoot { get { return m_unitRoot; } }

    [SerializeField]
    private GameObject m_movementField;

#region level selection
    [Header("LevelSelection")]

    [SerializeField]
    private Transform m_levelSelectionRoot;

    [SerializeField]
    private GameObject m_levelSelectorPrefab;

    [SerializeField]
    private LevelSelectionRouteType m_levelSelectionRouteType;
    public LevelSelectionRouteType LevelSelectionRouteType { get { return m_levelSelectionRouteType; } }

    [SerializeField]
    private string m_levelNameToStart;

    [SerializeField]
    private int m_levelSelectionOrder;

    [SerializeField]
    private List<RouteMarkerMapping> m_levelSelectionRouteMarkerMappings;

    #endregion

    #region RouteMarker

    [Serializable]
    public class RouteMarkerMapping
    {
        public RouteMarkerType m_RouteMarkerType;
        public GameObject m_RouteMarkerPrefab;
    }

    [Header("RouteMarker")]
    [SerializeField]
    private List<RouteMarkerMapping> m_routeMarkerMappings;

    #endregion

    #region AttackRangeMarker

    [Serializable]
    public class AttackRangeMarkerMapping
    {
        public AreaTileType m_AreaTileType;
        public bool m_IsFilled;
        public Mesh m_AttackRangerMarkerPrefab;
    }

    [Header("AttackRangeMarker")]

    [SerializeField]
    private List<AttackRangeMarkerMapping> m_attackRangeMarkerMappings;

    [SerializeField]
    private MeshFilter m_attackRangeMeshFilter;

    private List<EnvironmentInstantiateHelper> m_environmentInstantiateHelper;
    private List<EnvironmentInstantiateHelper> EnvironmentInstantiateHelper
    {
        get
        {
            if (m_environmentInstantiateHelper == null)
            {
                m_environmentInstantiateHelper = new List<EnvironmentInstantiateHelper>(m_currentlyInstantiatedMapTiles.Count);

                foreach (var mapTile in m_currentlyInstantiatedMapTiles)
                {
                    m_environmentInstantiateHelper.Add(mapTile.GetComponent<EnvironmentInstantiateHelper>());
                }
            }

            return m_environmentInstantiateHelper;
        }
    }

    #endregion

    private List<GameObject> m_currentlyInstantiatedMapTiles = new List<GameObject>();
    private MapTileType m_currentInstantiatedMapTileType;

    private GameObject m_currentInstantiatedUnitGameObject;
    private MapGenerationData.Unit m_currentInstantiatedUnit;
    private GameObject m_currentInstantiateLevelSelector;

    private MapTileGeneratorEditor m_mapTileGeneratorEditor;
    private MapGenerationData.MapTile m_mapTileData;
    private MapTileGenerationService m_mapGenService;

    public Vector2 m_SimplifiedMapPosition;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying && Root.Instance.DebugValues.m_ShowCoordinatesOnNodes)
        {
            UnityEditor.Handles.Label(this.transform.position + Vector3.up,
                string.Format("X{0}, Y{1}", m_SimplifiedMapPosition.x, m_SimplifiedMapPosition.y), new GUIStyle
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState
                    {
                        textColor = Color.black
                    }
                });
        }
    }
#endif

    /// <summary>
    /// Creates the first MapTile child based on a default MapTileType.
    /// </summary>
    /// <param name="mapTileData">The map tile data.</param>
    /// <param name="simplifiedPosition">The simplified position of the maptile.</param>
    public void Initialize(ref MapGenerationData.MapTile mapTileData, Vector2 simplifiedPosition)
    {
        InitializeBaseValues(mapTileData);

        m_SimplifiedMapPosition = simplifiedPosition;

        ValidateMapTile();
        ValidateLevelSelector(true);
        ValidateUnitType(true, simplifiedPosition);
    }

    /// <summary>
    /// Initializes this BaseMapTile only visually.
    /// </summary>
    /// <param name="mapTileData">The map tile data.</param>
    public void InitializeVisually(MapGenerationData.MapTile mapTileData)
    {
        InitializeBaseValues(mapTileData);

        ValidateMapTile();
        ValidateLevelSelector(true);
        ValidateUnitType();
    }

    /// <summary>
    /// Initializes the base values.
    /// </summary>
    /// <param name="mapTileData">The map tile data.</param>
    private void InitializeBaseValues(MapGenerationData.MapTile mapTileData)
    {
        if (Application.isPlaying)
        {
            ControllerContainer.MonoBehaviourRegistry.TryGet(out m_mapTileGeneratorEditor);
        }
        else
        {
            m_mapTileGeneratorEditor = FindObjectOfType<MapTileGeneratorEditor>();
        }

        m_mapTileData = mapTileData;
        m_mapTileType = m_mapTileData.m_MapTileType;
        m_unitOnThisTile = m_mapTileData.m_Unit;
        m_levelSelectionRouteType = m_mapTileData.m_LevelSelectionRouteType;
        m_levelNameToStart = m_mapTileData.m_LevelNameToStart;
        m_levelSelectionOrder = m_mapTileData.m_LevelSelectionOrder;
        m_mapGenService = ControllerContainer.MapTileGenerationService;
    }

    /// <summary>
    /// Validates the specified map tile type.
    /// If the MapTileType has changed or the child was not created, will create the correct MapTile.
    /// </summary>
    public void ValidateMapTile()
    {
        if (m_currentInstantiatedMapTileType == m_mapTileType && m_currentlyInstantiatedMapTiles != null)
        {
            return;
        }

        if (m_currentlyInstantiatedMapTiles != null && m_currentlyInstantiatedMapTiles.Count > 0)
        {
            for (int i = m_currentlyInstantiatedMapTiles.Count - 1; i >= 0; i--)
            {
                DestroyImmediate(m_currentlyInstantiatedMapTiles[i]);
            }

            m_currentlyInstantiatedMapTiles = new List<GameObject>();
        }

        if (m_mapTileType == MapTileType.Empty)
        {
            return;
        }

        InstantiateMapTilePrefab();
    }

    /// <summary>
    /// Validates the type of the unit.
    /// </summary>
    /// <param name="registerUnit">if set to <c>true</c> [register unit].</param>
    /// <param name="simplifiedPosition">The simplified position of the unit.</param>
    public void ValidateUnitType(bool registerUnit = false, Vector2 simplifiedPosition = new Vector2())
    {
        if (m_currentInstantiatedUnit != null)
        {
            DestroyImmediate(m_currentInstantiatedUnitGameObject);
        }

        if (m_unitOnThisTile != null && m_unitOnThisTile.m_UnitType != UnitType.None)
        {
            InstantiateUnitPrefab(simplifiedPosition, registerUnit);
            m_mapTileData.m_Unit = m_unitOnThisTile;

            UpdateVisibilityOfEnvironment(true);
        }
    }

    /// <summary>
    /// Validates the state of the instantiated level selector based on the values set in the inspector.
    /// </summary>
    public void ValidateLevelSelector(bool forceCreation = false)
    {
        if (m_mapTileData == null || (!forceCreation && m_levelSelectionRouteType == m_mapTileData.m_LevelSelectionRouteType &&
            string.Equals(m_levelNameToStart, m_mapTileData.m_LevelNameToStart) && m_levelSelectionOrder == m_mapTileData.m_LevelSelectionOrder))
        {
            return;
        }

        if (m_currentInstantiateLevelSelector != null)
        {
            DestroyImmediate(m_currentInstantiateLevelSelector);
        }

        m_mapTileData.m_LevelSelectionRouteType = m_levelSelectionRouteType;
        m_mapTileData.m_LevelNameToStart = m_levelNameToStart;
        m_mapTileData.m_LevelSelectionOrder = m_levelSelectionOrder;

        if (m_levelSelectionRouteType == LevelSelectionRouteType.LevelSelector)
        {
            m_currentInstantiateLevelSelector = Instantiate(m_levelSelectorPrefab);
            m_currentInstantiateLevelSelector.transform.SetParent(m_levelSelectionRoot);
            m_currentInstantiateLevelSelector.transform.localPosition = Vector3.zero;
            m_currentInstantiateLevelSelector.transform.localScale = Vector3.one;

            m_currentInstantiateLevelSelector.GetComponent<LevelSelector>().Initialize(m_mapTileData.m_LevelNameToStart, m_levelSelectionOrder, this);
        }           
    }

    /// <summary>
    /// Updates the visibility of all environment props of all EnvironmentInstantiateHelper of this maptile.
    /// </summary>
    /// <param name="visible">if set to <c>true</c> sets all props visible; otherwise invisible.</param>
    public void UpdateVisibilityOfEnvironment(bool visible)
    {
        if (EnvironmentInstantiateHelper == null || EnvironmentInstantiateHelper.Count <= 0)
        {
            return;
        }

        foreach (var environmentInstantiateHelper in EnvironmentInstantiateHelper)
        {
            if (environmentInstantiateHelper != null)
            {
                environmentInstantiateHelper.UpdateVisibilityOfEnvironment(visible);
            }
        }
    }

    /// <summary>
    /// Instantiates the map tile prefab.
    /// </summary>
    /// <returns></returns>
    private void InstantiateMapTilePrefab()
    {
        if (m_mapTileGeneratorEditor == null)
        {
            return;
        }

        m_currentInstantiatedMapTileType = m_mapTileType;
        m_mapTileData.m_MapTileType = m_mapTileType;

        List<CardinalDirection> adjacentWaterDirections;

        if (m_mapTileType != MapTileType.Water && m_mapGenService.IsMapTileNextToType(MapTileType.Water, m_SimplifiedMapPosition, m_mapTileGeneratorEditor.CurrentlyVisibleMap, out adjacentWaterDirections))
        {
            InstantiateComplexBorderMapTile(adjacentWaterDirections);
            InstantiateEnvironment();
        }
        else
        {
            // Instantiate MapTile
            GameObject mapTilePrefabToInstantiate = m_mapTileGeneratorEditor.GetPrefabOfMapTileType(m_mapTileType);

            if (mapTilePrefabToInstantiate != null)
            {
                InstantiateMapTile(mapTilePrefabToInstantiate);
                InstantiateEnvironment();
            }
            else
            {
                Debug.LogErrorFormat("MapTile with Type: '{0}' was not found!", m_mapTileType);
                m_currentInstantiatedMapTileType = MapTileType.Empty;
            }
        }
    }

    /// <summary>
    /// Instantiates four corner prefabs that correctly face adjacent water maptiles.
    /// </summary>
    /// <param name="adjacentWaterDirections">The directions the water is adjacent.</param>
    private void InstantiateComplexBorderMapTile(List<CardinalDirection> adjacentWaterDirections)
    {
        foreach (var borderDirection in m_mapGenService.GetBorderDirections(adjacentWaterDirections))
        {
            MapTileBorderPrefabData positionAndRotationForBorder = new MapTileBorderPrefabData(m_mapTileGeneratorEditor.GetMapTileBorderPrefab(m_mapTileType, borderDirection.Value));

            ApplyPositionAndRotationToBorderData(ref positionAndRotationForBorder, borderDirection.Key, borderDirection.Value);

            InstantiateMapTile(positionAndRotationForBorder.Prefab, positionAndRotationForBorder.Position, positionAndRotationForBorder.Rotation);
        }
    }

    /// <summary>
    /// Returns the position and rotation of a border based on the given direction it should face.
    /// </summary>
    private void ApplyPositionAndRotationToBorderData(ref MapTileBorderPrefabData borderPrefabData, CardinalDirection direction, MapTileBorderType borderType)
    {
        borderPrefabData.Position = Vector3.zero;
        borderPrefabData.Rotation = Vector3.zero;

        switch (direction)
        {
            case CardinalDirection.NorthEast:
            case CardinalDirection.East:
                borderPrefabData.Rotation = new Vector3(0f, 0f, 0f);

                if (borderType == MapTileBorderType.StraightRightAligned)
                {
                    borderPrefabData.Position = new Vector3(0f, 0f, 1f);
                }
                break;
            case CardinalDirection.NorthWest:
            case CardinalDirection.North:
                borderPrefabData.Rotation = new Vector3(0f, 270f, 0f);

                if (borderType == MapTileBorderType.StraightRightAligned)
                {
                    borderPrefabData.Position = new Vector3(-1f, 0f, 0f);
                }
                break;
            case CardinalDirection.SouthEast:
            case CardinalDirection.South:
                borderPrefabData.Rotation = new Vector3(0f, 90f, 0f);

                if (borderType == MapTileBorderType.StraightRightAligned)
                {
                    borderPrefabData.Position = new Vector3(1f, 0f, 0f);
                }
                break;
            case CardinalDirection.SouthWest:
            case CardinalDirection.West:
                borderPrefabData.Rotation = new Vector3(0f, 180f, 0f);

                if (borderType == MapTileBorderType.StraightRightAligned)
                {
                    borderPrefabData.Position = new Vector3(0f, 0f, -1f);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException("direction", direction, null);
        }
    }

    /// <summary>
    /// Instantiates a simple singular maptile prefab including the environment at the default position and with the default rotation.
    /// </summary>
    /// <param name="mapTilePrefabToInstantiate">The maptile prefab to instantiate.</param>
    private void InstantiateMapTile(GameObject mapTilePrefabToInstantiate)
    {
        InstantiateMapTile(mapTilePrefabToInstantiate, Vector3.zero, Vector3.zero);
    }

    /// <summary>
    /// Instantiates a simple singular maptile prefab including the environment.
    /// </summary>
    /// <param name="mapTilePrefabToInstantiate">The maptile prefab to instantiate.</param>
    /// <param name="position">The local position the prefab should be set to.</param>
    /// <param name="rotation">The local rotation the prefab should be set to.</param>
    private void InstantiateMapTile(GameObject mapTilePrefabToInstantiate, Vector3 position, Vector3 rotation)
    {
        if (mapTilePrefabToInstantiate == null)
        {
            Debug.LogWarning("Prefab to instantiate was null!");
            return;
        }

        GameObject instantiatedMapTile = Instantiate(mapTilePrefabToInstantiate);
        instantiatedMapTile.transform.SetParent(this.transform);
        instantiatedMapTile.transform.localPosition = position;
        instantiatedMapTile.transform.localRotation = Quaternion.Euler(rotation);

        m_currentlyInstantiatedMapTiles.Add(instantiatedMapTile);
    }

    /// <summary>
    /// Instantiates the environment on all currently instantiated maptile prefabs.
    /// </summary>
    private void InstantiateEnvironment()
    {
        if (EnvironmentInstantiateHelper != null && EnvironmentInstantiateHelper.Count > 0)
        {
            foreach (var environmentInstantiateHelper in EnvironmentInstantiateHelper)
            {
                if (environmentInstantiateHelper != null)
                {
                    environmentInstantiateHelper.InstantiateEnvironment();
                }
            }
        }
    }

    /// <summary>
    /// Instantiates the unit prefab.
    /// </summary>
    /// <param name="simplifiedPosition">The simplified position of the unit.</param>
    /// <param name="registerUnit">if set to <c>true</c> [register unit].</param>
    private void InstantiateUnitPrefab(Vector2 simplifiedPosition, bool registerUnit)
    {
        if (m_mapTileGeneratorEditor == null)
        {
            return;
        }

        // Instantiate UnitType
        GameObject unitPrefabToInstantiate = m_mapTileGeneratorEditor.BaseUnitPrefab;

        if (unitPrefabToInstantiate != null)
        {
            m_currentInstantiatedUnitGameObject = Instantiate(unitPrefabToInstantiate);
            m_currentInstantiatedUnitGameObject.transform.SetParent(m_unitRoot);
            m_currentInstantiatedUnitGameObject.transform.localPosition = Vector3.zero;

            BaseUnit baseUnit = m_currentInstantiatedUnitGameObject.GetComponent<BaseUnit>();

            if (baseUnit != null)
            {
                baseUnit.Initialize(m_unitOnThisTile, m_mapTileGeneratorEditor.GetMeshOfUnitType(m_unitOnThisTile.m_UnitType), simplifiedPosition, registerUnit);

                baseUnit.SetTeamColorMaterial(m_mapTileGeneratorEditor.GetMaterialForTeamColor(m_unitOnThisTile.m_UnitType, m_unitOnThisTile.m_TeamColor));

                baseUnit.SetRotation(m_unitOnThisTile.m_Orientation);
            }

            m_currentInstantiatedUnit = m_unitOnThisTile;
        }
    }

    /// <summary>
    /// Changes the visibility of movement field.
    /// </summary>
    /// <param name="setVisibilityTo">if set to <c>true</c> [set visibility to].</param>
    public void ChangeVisibilityOfMovementField(bool setVisibilityTo)
    {
        m_movementField.SetActive(setVisibilityTo);

        ShaderBlinkOrchestrator shaderBlinkOrchestrator = null;

        if (ControllerContainer.MonoBehaviourRegistry.TryGet(out shaderBlinkOrchestrator))
        {
            if (setVisibilityTo)
            {
                shaderBlinkOrchestrator.AddRendererToBlink(m_SimplifiedMapPosition, m_movementField.GetComponent<MeshRenderer>());
            }
            else
            {
                shaderBlinkOrchestrator.RemoveRenderer(m_SimplifiedMapPosition);
            }
        }
    }

    /// <summary>
    /// Hides a previously displayed attack range marker.
    /// </summary>
    public void HideAttackRangeMarker()
    {
        m_attackRangeMeshFilter.gameObject.SetActive(false);
    }

    /// <summary>
    /// Display attack range marker with the correct borders and rotation.
    /// </summary>
    /// <param name="attackableMapTiles">The maptiles attack by the selected unit at the position of the unit or the selected position.</param>
    /// <param name="attackRangeCenterPosition">The center position of the attack range border.</param>
    public void DisplayAttackRangeMarker(List<BaseMapTile> attackableMapTiles, Vector2 attackRangeCenterPosition)
    {
        m_attackRangeMeshFilter.gameObject.SetActive(true);

        List<Vector2> adjacentNodes = ControllerContainer.TileNavigationController.GetAdjacentNodes(m_SimplifiedMapPosition);
        List<BaseMapTile> adjacentAttackableTiles = new List<BaseMapTile>();

        for (int i = 0; i < adjacentNodes.Count; i++)
        {
            BaseMapTile adjacentAttackableTile = attackableMapTiles.Find(mapTile => mapTile.m_SimplifiedMapPosition == adjacentNodes[i]);

            if (adjacentAttackableTile != null)
            {
                adjacentAttackableTiles.Add(adjacentAttackableTile);
            }
        }

        AreaTileType areaTileType = AreaTileType.NoBorders;

        switch (adjacentAttackableTiles.Count)
        {
            case 0:
                return;

            case 1:
                areaTileType = AreaTileType.ThreeBorders;
                break;
            case 2:
                areaTileType = ControllerContainer.MapTileGenerationService.GetTwoBorderAreaTileType(m_SimplifiedMapPosition, adjacentAttackableTiles);

                break;
            case 3:
                areaTileType = AreaTileType.OneBorder;

                break;

            case 4:
                m_attackRangeMeshFilter.gameObject.SetActive(false);
                return;
        }

        float yRotation = ControllerContainer.MapTileGenerationService.GetAttackMarkerBorderRotation(m_SimplifiedMapPosition, areaTileType, adjacentNodes, adjacentAttackableTiles, attackRangeCenterPosition);

        m_attackRangeMeshFilter.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        AttackRangeMarkerMapping uvCoordinateMeshToUse = m_attackRangeMarkerMappings.Find(mapping => mapping.m_AreaTileType == areaTileType && !mapping.m_IsFilled);

        if (uvCoordinateMeshToUse != null)
        {
            m_attackRangeMeshFilter.mesh = uvCoordinateMeshToUse.m_AttackRangerMarkerPrefab;
        }
    }

    /// <summary>
    /// Hides all route marker.
    /// </summary>
    public void HideAllRouteMarker()
    {
        for (int routeMarkerIndex = 0; routeMarkerIndex < m_routeMarkerMappings.Count; routeMarkerIndex++)
        {
            m_routeMarkerMappings[routeMarkerIndex].m_RouteMarkerPrefab.SetActive(false);
        }
    }

    /// <summary>
    /// Displays the route marker.
    /// </summary>
    /// <param name="routeMarkerDefinition">The route marker definition.</param>
    public void DisplayRouteMarker(RouteMarkerDefinition routeMarkerDefinition)
    {
        ChangeVisibilityOfMovementField(false);

        RouteMarkerMapping routeMarkerMappingToUse = m_routeMarkerMappings.Find(routeMarkerMapping => routeMarkerMapping.m_RouteMarkerType == routeMarkerDefinition.RouteMarkerType);

        if (routeMarkerMappingToUse != null)
        {
            routeMarkerMappingToUse.m_RouteMarkerPrefab.SetActive(true);
            routeMarkerMappingToUse.m_RouteMarkerPrefab.transform.rotation = Quaternion.Euler(routeMarkerDefinition.Rotation);
        }
    }

    /// <summary>
    /// Instantiates a part of the level selection route.
    /// </summary>
    public void InstantiateLevelSelectionRoute(RouteMarkerDefinition routeMarkerDefinition)
    {
        if (routeMarkerDefinition.RouteMarkerType == RouteMarkerType.Destination)
        {
            return;
        }

        RouteMarkerMapping routeMarkerMappingToUse = m_levelSelectionRouteMarkerMappings.Find(
            routeMarkerMapping => routeMarkerMapping.m_RouteMarkerType == routeMarkerDefinition.RouteMarkerType);

        GameObject instatiatedRoute = Instantiate(routeMarkerMappingToUse.m_RouteMarkerPrefab, m_levelSelectionRoot);
        instatiatedRoute.transform.localPosition = Vector3.zero;
        instatiatedRoute.transform.rotation = Quaternion.Euler(routeMarkerDefinition.Rotation);
    }
}
