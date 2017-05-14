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

    private EnvironmentInstantiateHelper m_environmentInstantiateHelper;
    public EnvironmentInstantiateHelper EnvironmentInstantiateHelper
    {
        get
        {
            return m_environmentInstantiateHelper ?? 
                (m_environmentInstantiateHelper = m_currentInstantiatedMapTile.GetComponent<EnvironmentInstantiateHelper>());
        }
    }

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

            if (EnvironmentInstantiateHelper != null)
            {
                EnvironmentInstantiateHelper.UpdateVisibilityOfEnvironment(true);
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

            if (EnvironmentInstantiateHelper != null)
            {
                EnvironmentInstantiateHelper.InstantiateEnvironment();   
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

        ShaderBlinkOrchestrator shaderBlinkOrchestrator = null;

        if (ControllerContainer.MonoBehaviourRegistry.TryGet(out shaderBlinkOrchestrator))
        {
            if (setVisibilityTo)
            {
                shaderBlinkOrchestrator.AddRendererToBlink(SimplifiedMapPosition, m_movementField.GetComponent<MeshRenderer>());
            }
            else
            {
                shaderBlinkOrchestrator.RemoveRenderer(SimplifiedMapPosition);
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

        List<Vector2> adjacentNodes = ControllerContainer.TileNavigationController.GetAdjacentNodes(SimplifiedMapPosition);
        List<BaseMapTile> adjacentAttackableTiles = new List<BaseMapTile>();

        for (int i = 0; i < adjacentNodes.Count; i++)
        {
            BaseMapTile adjacentAttackableTile = attackableMapTiles.Find(mapTile => mapTile.SimplifiedMapPosition == adjacentNodes[i]);

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
                areaTileType = GetTwoBorderAreaTileType(adjacentAttackableTiles);
                
                break;
            case 3:
                areaTileType = AreaTileType.OneBorder;

                break;

            case 4:
                m_attackRangeMeshFilter.gameObject.SetActive(false);
                return;
        }

        float yRotation = GetAttackMarkerBorderRotation(areaTileType, adjacentNodes, 
            adjacentAttackableTiles, attackRangeCenterPosition);

        m_attackRangeMeshFilter.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        AttackRangeMarkerMapping uvCoordinateMeshToUse = m_attackRangeMarkerMappings.Find(mapping => mapping.m_AreaTileType == areaTileType && !mapping.m_IsFilled);

        if (uvCoordinateMeshToUse != null)
        {
            m_attackRangeMeshFilter.mesh = uvCoordinateMeshToUse.m_AttackRangerMarkerPrefab;
        }
    }

    /// <summary>
    /// Returns the either the corner or the straight version of the areatiletype depending on the given adjacent attackable tiles.
    /// </summary>
    /// <param name="adjacentAttackableTiles">The adjacent attackable tiles to base the selection on.</param>
    private AreaTileType GetTwoBorderAreaTileType(List<BaseMapTile> adjacentAttackableTiles)
    {
        AreaTileType twoBorderAreaTileType = AreaTileType.NoBorders;

        // We have to assume 2 as a count here, because otherwise this method was called incorrectly.
        if (adjacentAttackableTiles.Count == 2)
        {
            Vector2 diffToFirstAdjacentTile = SimplifiedMapPosition - adjacentAttackableTiles[0].SimplifiedMapPosition;
            Vector2 diffToSecondAdjacentTile = SimplifiedMapPosition - adjacentAttackableTiles[1].SimplifiedMapPosition;

            Vector2 combindedDiff = diffToFirstAdjacentTile - diffToSecondAdjacentTile;

            twoBorderAreaTileType = Mathf.Abs((int) combindedDiff.x) == 1 && Mathf.Abs((int)combindedDiff.y) == 1 ? 
                AreaTileType.TwoBordersCorner :
                AreaTileType.TwoBorderStraight;
        }

        return twoBorderAreaTileType;
    }

    /// <summary>
    /// Returns the attack marker border rotation.
    /// </summary>
    /// <param name="areaTileType">The areaTileType of the attack marker.</param>
    /// <param name="adjacentNodes">The adjacent nodes.</param>
    /// <param name="adjacentAttackableTiles">The adjacent attackable tiles.</param>
    /// <param name="attackRangeCenterPosition">The center position of the attack range border.</param>
    private float GetAttackMarkerBorderRotation(AreaTileType areaTileType, List<Vector2> adjacentNodes, 
        List<BaseMapTile> adjacentAttackableTiles, Vector2 attackRangeCenterPosition)
    {
        Vector2 nodePositionDiff = Vector2.zero;

        switch (areaTileType)
        {
            case AreaTileType.OneBorder:
                nodePositionDiff = SimplifiedMapPosition - adjacentNodes.Find(
                    node => !adjacentAttackableTiles.Exists(tile => tile.SimplifiedMapPosition == node));
                break;
            case AreaTileType.TwoBordersCorner:
                return GetTwoBorderCornerRotation(adjacentAttackableTiles, attackRangeCenterPosition);
            case AreaTileType.TwoBorderStraight:

                break;
            case AreaTileType.ThreeBorders:
                // There is only one adjacent attackable tile
                nodePositionDiff = adjacentAttackableTiles[0].SimplifiedMapPosition - SimplifiedMapPosition;
                break;
        }

        return ControllerContainer.TileNavigationController.GetRotationFromCardinalDirection(
            ControllerContainer.TileNavigationController.GetCardinalDirectionFromNodePositionDiff(nodePositionDiff, false));
    }

    /// <summary>
    /// Returns the attack range marker rotation for two border corner attack range marker.
    /// </summary>
    /// <param name="adjacentAttackableTiles">The adjacent attackable tiles.</param>
    /// <param name="attackRangeCenterPosition">The center position of the attack range border.</param>
    private float GetTwoBorderCornerRotation(List<BaseMapTile> adjacentAttackableTiles, Vector2 attackRangeCenterPosition)
    {
        float rotationToReturn = 0f;

        Vector2 diffToFirstAdjacentTile = SimplifiedMapPosition - adjacentAttackableTiles[0].SimplifiedMapPosition;
        Vector2 diffToSecondAdjacentTile = SimplifiedMapPosition - adjacentAttackableTiles[1].SimplifiedMapPosition;

        Vector2 combindedDiff = diffToFirstAdjacentTile - diffToSecondAdjacentTile;

        if (combindedDiff.Equals(new Vector2(1, -1)))
        {
            rotationToReturn = 90f;
        }
        else if (combindedDiff.Equals(new Vector2(-1, 1)))
        {
            rotationToReturn = 270f;
        }
        else if (SimplifiedMapPosition.x > attackRangeCenterPosition.x)
        {
            rotationToReturn = 180f;
        }

        return rotationToReturn;
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
}
