using System;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable 0649
/// <summary>
/// Represents a parent object of a maptile prefab.
/// Controls which type of maptile is active as a child.
/// </summary>
public class BaseMapTile : MonoBehaviour
{
    [SerializeField] private MapTileType m_mapTileType;

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

    [Serializable]
    public class RouteMarkerMapping
    {
        public RouteMarkerType m_RouteMarkerType;
        public GameObject m_RouteMarkerPrefab;
    }

    [SerializeField]
    private List<RouteMarkerMapping> m_routeMarkerMappings;

    private GameObject m_currentInstantiatedMapTile;
    private MapTileType m_currentInstantiatedMapTileType;

    private GameObject m_currentInstantiatedUnitGameObject;
    private MapGenerationData.Unit m_currentInstantiatedUnit;

    private MapTileGeneratorEditor m_mapTileGeneratorEditor;
    private MapGenerationData.MapTile m_mapTileData;

    public Vector2 SimplifiedMapPosition { get; private set; }

    /// <summary>
    /// Creates the first MapTile child based on a default MapTileType.
    /// </summary>
    /// <param name="mapTileData">The map tile data.</param>
    /// <param name="simplifiedPosition">The simplified position of the maptile.</param>
    public void Initialize(ref MapGenerationData.MapTile mapTileData, Vector2 simplifiedPosition)
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

        SimplifiedMapPosition = simplifiedPosition;

        ValidateMapTile();
        ValidateUnitType(simplifiedPosition);
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
    /// <param name="simplifiedPosition">The simplified position of the unit.</param>
    public void ValidateUnitType(Vector2 simplifiedPosition = new Vector2())
    {
        if (m_currentInstantiatedUnit != null)
        {
            DestroyImmediate(m_currentInstantiatedUnitGameObject);
        }

        if (m_unitOnThisTile != null && m_unitOnThisTile.m_UnitType != UnitType.None)
        {
            InstantiateUnitPrefab(simplifiedPosition);
            m_mapTileData.m_Unit = m_unitOnThisTile;
        }
    }

    /// <summary>
    /// Instantiates the map tile prefab.
    /// </summary>
    /// <returns></returns>
    private void InstantiateMapTilePrefab()
    {
        // Instantiate MapTile
        GameObject mapTilePrefabToInstantiate = m_mapTileGeneratorEditor.GetPrefabOfMapTileType(m_mapTileType);

        if (mapTilePrefabToInstantiate != null)
        {
            m_currentInstantiatedMapTile = Instantiate(mapTilePrefabToInstantiate);
            m_currentInstantiatedMapTile.transform.SetParent(this.transform);
            m_currentInstantiatedMapTile.transform.localPosition = Vector3.zero;

            m_currentInstantiatedMapTileType = m_mapTileType;
            m_mapTileData.m_MapTileType = m_mapTileType;
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
    private void InstantiateUnitPrefab(Vector2 simplifiedPosition)
    {
        // Instantiate UnitType
        GameObject unitPrefabToInstantiate = m_mapTileGeneratorEditor.GetPrefabOfUnitType(m_unitOnThisTile.m_UnitType);

        if (unitPrefabToInstantiate != null)
        {
            m_currentInstantiatedUnitGameObject = Instantiate(unitPrefabToInstantiate);
            m_currentInstantiatedUnitGameObject.transform.SetParent(m_unitRoot);
            m_currentInstantiatedUnitGameObject.transform.localPosition = Vector3.zero;

            BaseUnit baseUnit = m_currentInstantiatedUnitGameObject.GetComponent<BaseUnit>();

            if (baseUnit != null)
            {
                baseUnit.Initialize(m_unitOnThisTile, simplifiedPosition);

                baseUnit.SetTeamColorUVMesh(m_mapTileGeneratorEditor.GetUVCoordinateMeshForTeamColor(
                    m_unitOnThisTile.m_UnitType, m_unitOnThisTile.m_Team.m_TeamColor));
            }

            m_currentInstantiatedUnit = m_unitOnThisTile;
        }
    }

    /// <summary>
    /// Changes the visibilty of movement field.
    /// </summary>
    /// <param name="setVisibiltyTo">if set to <c>true</c> [set visibilty to].</param>
    public void ChangeVisibiltyOfMovementField(bool setVisibiltyTo)
    {
        m_movementField.SetActive(setVisibiltyTo);
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
        ChangeVisibiltyOfMovementField(false);

        RouteMarkerMapping routeMarkerMappingToUse = m_routeMarkerMappings.Find(
            routeMarkerMapping => routeMarkerMapping.m_RouteMarkerType == routeMarkerDefinition.RouteMarkerType);

        if (routeMarkerMappingToUse != null)
        {
            routeMarkerMappingToUse.m_RouteMarkerPrefab.SetActive(true);
            routeMarkerMappingToUse.m_RouteMarkerPrefab.transform.rotation = Quaternion.Euler(routeMarkerDefinition.Rotation);
        }
    }
}
