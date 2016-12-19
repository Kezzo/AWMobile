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
    public MapTileType MapTileType { get { return m_mapTileType; } set { m_mapTileType = value; } }

    [SerializeField]
    private UnitType m_unitTypeOnThisTile;

    [SerializeField]
    private Transform m_unitRoot;

    private GameObject m_currentInstantiatedMapTile;
    private MapTileType m_currentInstantiatedMapTileType;

    private GameObject m_currentInstantiatedUnit;
    private UnitType m_currentInstantiatedUnitType;

    private MapTileGeneratorEditor m_mapTileGeneratorEditor;
    private MapGenerationData.MapTile m_mapTileData;


    /// <summary>
    /// Creates the first MapTile child based on a default MapTileType.
    /// </summary>
    public void Initialize(ref MapGenerationData.MapTile mapTileData)
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
        m_mapTileType = m_mapTileData.MapTileType;
        m_unitTypeOnThisTile = m_mapTileData.UnitType;

        ValidateMapTile();
        ValidateUnitType();
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
    public void ValidateUnitType()
    {
        if (m_currentInstantiatedUnitType == m_unitTypeOnThisTile && m_currentInstantiatedUnit != null)
        {
            return;
        }

        if (m_currentInstantiatedUnit != null)
        {
            DestroyImmediate(m_currentInstantiatedUnit);
        }

        if (m_unitTypeOnThisTile == UnitType.None)
        {
            m_mapTileData.UnitType = m_unitTypeOnThisTile;
        }
        else
        {
            InstantiateUnitPrefab();
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
            m_mapTileData.MapTileType = m_mapTileType;
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
    private void InstantiateUnitPrefab()
    {
        // Instantiate UnitType
        GameObject unitPrefabToInstantiate = m_mapTileGeneratorEditor.GetPrefabOfUnitType(m_unitTypeOnThisTile);

        if (unitPrefabToInstantiate != null)
        {
            m_currentInstantiatedUnit = Instantiate(unitPrefabToInstantiate);
            m_currentInstantiatedUnit.transform.SetParent(m_unitRoot);
            m_currentInstantiatedUnit.transform.localPosition = Vector3.zero;

            m_currentInstantiatedUnitType = m_unitTypeOnThisTile;
            m_mapTileData.UnitType = m_unitTypeOnThisTile;
        }
        else
        {
            Debug.LogErrorFormat("Unit with Type: '{0}' was not found!", m_unitTypeOnThisTile);
        }
    }
}
