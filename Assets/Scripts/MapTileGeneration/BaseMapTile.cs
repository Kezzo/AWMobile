﻿using System;
using System.Collections.Generic;
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

    #region RouteMarker

    [Serializable]
    public class RouteMarkerMapping
    {
        public RouteMarkerType m_RouteMarkerType;
        public GameObject m_RouteMarkerPrefab;
    }

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

    [SerializeField]
    private List<AttackRangeMarkerMapping> m_attackRangeMarkerMappings;

    [SerializeField]
    private MeshFilter m_attackRangeMeshFilter;

    #endregion

    private GameObject m_currentInstantiatedMapTile;
    private MapTileType m_currentInstantiatedMapTileType;

    private GameObject m_currentInstantiatedUnitGameObject;
    private MapGenerationData.Unit m_currentInstantiatedUnit;

    private MapTileGeneratorEditor m_mapTileGeneratorEditor;
    private MapGenerationData.MapTile m_mapTileData;

    public Vector2 SimplifiedMapPosition { get; private set; }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (!Root.Instance.DebugValues.m_ShowCoordinatesOnNodes)
            {
                return;
            }

            UnityEditor.Handles.Label(this.transform.position + Vector3.up,
                string.Format("X{0}, Y{1}", SimplifiedMapPosition.x, SimplifiedMapPosition.y), new GUIStyle
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

        SimplifiedMapPosition = simplifiedPosition;

        ValidateMapTile();
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
    }

    /// <summary>
    /// Validates the specified map tile type.
    /// If the MapTileType has changed or the child was not created, will create the correct MapTile.
    /// </summary>
    public void ValidateMapTile()
    {
        if (m_currentInstantiatedMapTileType == m_mapTileType && m_currentInstantiatedMapTile != null)
        {
            return;
        }

        if (m_currentInstantiatedMapTile != null)
        {
            DestroyImmediate(m_currentInstantiatedMapTile);
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

        // Instantiate MapTile
        GameObject mapTilePrefabToInstantiate = m_mapTileGeneratorEditor.GetPrefabOfMapTileType(m_mapTileType);

        if (mapTilePrefabToInstantiate != null)
        {
            m_currentInstantiatedMapTile = Instantiate(mapTilePrefabToInstantiate);
            m_currentInstantiatedMapTile.transform.SetParent(this.transform);
            m_currentInstantiatedMapTile.transform.localPosition = Vector3.zero;
            m_currentInstantiatedMapTile.transform.localRotation = Quaternion.Euler(Vector3.zero);

            m_currentInstantiatedMapTileType = m_mapTileType;
            m_mapTileData.m_MapTileType = m_mapTileType;

            EnvironmentInstantiateHelper environmentInstantiateHelper =
                m_currentInstantiatedMapTile.GetComponent<EnvironmentInstantiateHelper>();

            if (environmentInstantiateHelper != null)
            {
                environmentInstantiateHelper.InstantiateEnvironment();   
            }
        }
        else
        {
            Debug.LogErrorFormat("MapTile with Type: '{0}' was not found!", m_mapTileType);

            m_currentInstantiatedMapTileType = MapTileType.Empty;
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
            m_currentInstantiatedMapTile.transform.localRotation = Quaternion.Euler(Vector3.zero);

            BaseUnit baseUnit = m_currentInstantiatedUnitGameObject.GetComponent<BaseUnit>();

            if (baseUnit != null)
            {
                baseUnit.Initialize(m_unitOnThisTile, m_mapTileGeneratorEditor.GetMeshOfUnitType(m_unitOnThisTile.m_UnitType), 
                    simplifiedPosition, registerUnit);

                baseUnit.SetTeamColorMaterial(m_mapTileGeneratorEditor.GetMaterialForTeamColor(
                    m_unitOnThisTile.m_UnitType, m_unitOnThisTile.m_TeamColor));

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
    }

    /// <summary>
    /// Changes the visibility of attack range marker.
    /// </summary>
    /// <param name="setVisibilityTo">if set to <c>true</c> [set visibility to].</param>
    /// <param name="attackableMapTiles">The maptiles attack by the selected unit at the position of the unit or the selected position.</param>
    public void ChangeVisibilityOfAttackRangeMarker(bool setVisibilityTo, List<BaseMapTile> attackableMapTiles = null)
    {
        m_attackRangeMeshFilter.gameObject.SetActive(setVisibilityTo);
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

        RouteMarkerMapping routeMarkerMappingToUse = m_routeMarkerMappings.Find(
            routeMarkerMapping => routeMarkerMapping.m_RouteMarkerType == routeMarkerDefinition.RouteMarkerType);

        if (routeMarkerMappingToUse != null)
        {
            routeMarkerMappingToUse.m_RouteMarkerPrefab.SetActive(true);
            routeMarkerMappingToUse.m_RouteMarkerPrefab.transform.rotation = Quaternion.Euler(routeMarkerDefinition.Rotation);
        }
    }
}
