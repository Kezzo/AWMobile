#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using System;
using System.Collections.Generic;
using AWM.BattleVisuals;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AWM.MapTileGeneration
{
    /// <summary>
    /// Class to generation a map from maptiles in the editor.
    /// </summary>
    [ExecuteInEditMode]
    public class MapTileGeneratorEditor : MonoBehaviour
    {
        [SerializeField]
        private string m_levelToEdit;

        [SerializeField]
        private Vector2 m_levelSize;

        [SerializeField]
        private float m_tileMargin;

        [Header("Data Setup")]

        [SerializeField]
        private GameObject m_tilePrefab;

        [SerializeField]
        private Transform m_levelRoot;

        [SerializeField]
        private GameObject m_baseUnitPrefab;
        public GameObject BaseUnitPrefab { get { return m_baseUnitPrefab; } }

        [SerializeField]
        private CloudShadowOrchestrator m_cloudShadowOrchestrator;

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

        [Header("Map Creation Hotkey Setup")]

        [SerializeField]
        private List<QuickTileTypeSwitchHotKey> m_quickTileTypeSwitcherHotKeys;

        [Serializable]
        public class QuickTileTypeSwitchHotKey
        {
            public KeyCode m_KeyCode;
            public MapTileType m_MapTileType;
        }

        [SerializeField]
        private List<QuickUnitTypeSwitchHotKey> m_quickUnitTypeSwitcherHotKeys;

        [Serializable]
        public class QuickUnitTypeSwitchHotKey
        {
            public KeyCode m_KeyCode;
            public UnitType m_UnitType;
        }

        [SerializeField]
        private List<QuickUnitRotationSwitchHotKey> m_quickUnitRotationSwitcherHotKeys;

        [Serializable]
        public class QuickUnitRotationSwitchHotKey
        {
            public KeyCode m_KeyCode;
            public CardinalDirection m_CardinalDirection;
        }

        [SerializeField]
        private LayerMask m_mapTileSelectorLayer;

        [SerializeField]
        private Camera m_mapTileSelectionCamera;

        public bool HotkeysActive { get; set; }
        public MapTileType m_LastToggledTileType = MapTileType.Empty;
        public UnitType m_LastToggledUnitType = UnitType.None;
        public TypeToEdit m_CurrentTypeToEdit = TypeToEdit.None;

        public TeamColor m_CurrentTeamColor = TeamColor.Blue;

        public enum TypeToEdit
        {
            None = 0,
            MapTileType = 1,
            UnitType = 2
        }

        private BaseMapTile m_lastUpdateMapTile;

        private MapGenerationData m_currentlyVisibleMap;
        public MapGenerationData CurrentlyVisibleMap { get { return m_currentlyVisibleMap; } }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                CC.MBR.Register(this);
            }
        }

#region live map editing

        private void OnGUI()
        {
            if (Application.isPlaying || !HotkeysActive || Event.current == null)
            {
                return;
            }

            if (Event.current.type == EventType.KeyDown)
            {
                KeyCode keyCode = Event.current.keyCode;

                if (!TryToUpdateRotationOfLastUnit(keyCode))
                {
                    TryToUpdateToggledTypes(keyCode);
                }
            }
            else if (MouseInputHelper.GetMouseButton(1))
            {
                RaycastHit raycastHit;
                if (!TryToRaycastMapTileColliderAtMouse(Event.current.mousePosition, out raycastHit))
                {
                    return;
                }

                UpdateTypeOfMapTileAtWorldPosition(raycastHit.point);
            }
        }

        /// <summary>
        /// This will update the <see cref="MapTileType"/> of the <see cref="BaseMapTile"/> closest to the given world position.
        /// </summary>
        /// <param name="worldPosition">The world position to find the closest <see cref="BaseMapTile"/> for.</param>
        private void UpdateTypeOfMapTileAtWorldPosition(Vector3 worldPosition)
        {
            BaseMapTile closestBaseMapTile = null;
            float closestDistanceToRaycastHit = float.MaxValue;

            foreach (var instantiatedBaseMapTile in CC.TNC.MapTilePositions.Values)
            {
                float distanceToRaycastHit = Vector3.Distance(instantiatedBaseMapTile.transform.position, worldPosition);

                if (distanceToRaycastHit < closestDistanceToRaycastHit)
                {
                    closestBaseMapTile = instantiatedBaseMapTile;
                    closestDistanceToRaycastHit = distanceToRaycastHit;
                }
            }

            if (closestBaseMapTile == null)
            {
                return;
            }

            switch (m_CurrentTypeToEdit)
            {
                case TypeToEdit.MapTileType:
                    closestBaseMapTile.MapTileType = m_LastToggledTileType;
                    closestBaseMapTile.ValidateMapTile();

                    List<Vector2> adjacentNodes = CC.TNC.GetAdjacentNodes(
                        closestBaseMapTile.m_SimplifiedMapPosition, true);

                    foreach (var adjacentNode in adjacentNodes)
                    {
                        BaseMapTile mapTile;

                        if (CC.TNC.MapTilePositions.TryGetValue(adjacentNode,
                            out mapTile))
                        {
                            mapTile.ValidateMapTile(true, CC.TNC);
                        }
                    }

                    break;
                case TypeToEdit.UnitType:
                    closestBaseMapTile.Unit.m_UnitType = m_LastToggledUnitType;
                    closestBaseMapTile.Unit.m_TeamColor = m_CurrentTeamColor;
                    closestBaseMapTile.ValidateUnitType();
                    break;
            }

            m_lastUpdateMapTile = closestBaseMapTile;
        }

        /// <summary>
        /// Tries to raycast the maptile collider at the given mouse position.
        /// </summary>
        /// <param name="mousePosition">The mouse position at which the raycast should be done.</param>
        /// <param name="raycastHit">The result of the raycast. Is null when no collider was hit.</param>
        /// <returns>Returns true when a collider was hit, false otherwise.</returns>
        private bool TryToRaycastMapTileColliderAtMouse(Vector2 mousePosition, out RaycastHit raycastHit)
        {
            Ray selectionRay = Camera.main.ScreenPointToRay(new Vector3(mousePosition.x, Screen.height - mousePosition.y, 0f));

            if (Camera.main.orthographic)
            {
                selectionRay.direction = Camera.main.transform.forward.normalized;
            }

            Debug.DrawRay(selectionRay.origin, selectionRay.direction*100f, Color.yellow, 1f);

            return Physics.Raycast(selectionRay, out raycastHit, 100f, m_mapTileSelectorLayer);
        }

        /// <summary>
        /// Updates the <see cref="CardinalDirection"/> of the last placed unit.
        /// </summary>
        /// <param name="pressedKeyCode">Has do be a keycode defined in the unit rotation key mapping.</param>
        /// <returns>True when a keycode was pressed that is mapped to unit rotation changes; otherwise false.</returns>
        private bool TryToUpdateRotationOfLastUnit(KeyCode pressedKeyCode)
        {
            foreach (var quickUnitRotationSwitchHotKey in m_quickUnitRotationSwitcherHotKeys)
            {
                if (pressedKeyCode == quickUnitRotationSwitchHotKey.m_KeyCode)
                {
                    if (m_lastUpdateMapTile != null)
                    {
                        m_lastUpdateMapTile.Unit.m_Orientation = quickUnitRotationSwitchHotKey.m_CardinalDirection;
                        m_lastUpdateMapTile.ValidateUnitType();
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This method will try to update the currently toggled <see cref="MapTileType"/> or <see cref="UnitType"/> 
        /// that is used for quickly changing a <see cref="BaseMapTile"/>.
        /// </summary>
        private void TryToUpdateToggledTypes(KeyCode pressedKeyCode)
        {
            foreach (var quickTileTypeSwitcherHotKey in m_quickTileTypeSwitcherHotKeys)
            {
                if (pressedKeyCode == quickTileTypeSwitcherHotKey.m_KeyCode)
                {
                    m_LastToggledTileType = quickTileTypeSwitcherHotKey.m_MapTileType;
                    m_CurrentTypeToEdit = TypeToEdit.MapTileType;
                    return;
                }
            }

            foreach (var quickUnitTypeSwitchHotKey in m_quickUnitTypeSwitcherHotKeys)
            {
                if (pressedKeyCode == quickUnitTypeSwitchHotKey.m_KeyCode)
                {
                    m_LastToggledUnitType = quickUnitTypeSwitchHotKey.m_UnitType;
                    m_CurrentTypeToEdit = TypeToEdit.UnitType;
                    return;
                }
            }
        }

        #endregion

        /// <summary>
        /// Generates the map.
        /// </summary>
        public void GenerateMap()
        {
            ClearMap();
            m_currentlyVisibleMap = CC.MGS.GenerateMapGroups(m_levelSize, m_tileMargin, 2);
            CC.MGS.LoadGeneratedMap(m_currentlyVisibleMap, m_tilePrefab, m_levelRoot);
        }

        /// <summary>
        /// Loads an existing map.
        /// </summary>
        /// <param name="mapGenerationData">The map generation data used to generate the map.</param>
        public void LoadExistingMap(MapGenerationData mapGenerationData)
        {
            if (mapGenerationData == null)
            {
                Debug.LogErrorFormat("Given MapGenerationData is null! Unable to load existing map.");
            }
            else
            {
                ClearMap();
                m_currentlyVisibleMap = mapGenerationData;

                CC.MGS.LoadGeneratedMap(mapGenerationData, m_tilePrefab, m_levelRoot);

                if (Application.isPlaying)
                {
                    m_cloudShadowOrchestrator.GenerateCloudPool(m_currentlyVisibleMap.m_MapCloudShadowData);
                    m_cloudShadowOrchestrator.StartCloudShadowDisplay();
                }
            }
        }

        /// <summary>
        /// Loads a map generation data.
        /// </summary>
        /// <param name="levelNameToLoad">The level name to load.</param>
        public MapGenerationData LoadMapGenerationData(string levelNameToLoad = "")
        {
            string levelToLoad = Application.isPlaying ? levelNameToLoad : m_levelToEdit;

            string assetPath = string.Format("Levels/{0}", levelToLoad);

            MapGenerationData mapGenerationData = CC.ADS.GetAssetDataAtPath<MapGenerationData>(assetPath);

            if (mapGenerationData == null)
            {
                Debug.LogErrorFormat("There is no existing map with name: '{0}' in the path: '{1}'", levelToLoad, assetPath);
            }

            return mapGenerationData;
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
                MapTileBorderAssignment borderAssignment = mapTileTypeAssignment.m_MapTileBorders.Find(assignment => assignment.m_BorderType == mapTileBorderType);

                if (borderAssignment == null)
                {
                    Debug.LogFormat("MapTileBorderPrefab couldn't be found! MapTileType: {0} MapTileBorderType: {1}", mapTileType, mapTileBorderType);
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
                UnitTypeAssignment.TeamMaterialAssignment teamMaterialAssignment = unitTypeAssignment.m_ColorUvCoordinateAssignments.Find(material => material.m_TeamColor == teamColor);

                if (teamMaterialAssignment != null)
                {
                    materialToReturn = teamMaterialAssignment.m_Material;
                }
            }

            return materialToReturn;
        }
    }
}
