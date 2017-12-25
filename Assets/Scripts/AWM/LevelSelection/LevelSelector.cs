﻿using System.Collections;
using System.Collections.Generic;
using AWM.BattleMechanics;
using AWM.Controls;
using AWM.EditorAndDebugOnly;
using AWM.Enums;
using AWM.MapTileGeneration;
using AWM.System;
using UnityEngine;

namespace AWM.LevelSelection
{
    public class LevelSelector : MonoBehaviour
    {
        [SerializeField]
        private MeshRenderer m_levelFlagMeshRenderer;

        [SerializeField]
        private Material m_levelCompleteMaterial;

        [SerializeField]
        private Material m_levelNotCompleteMaterial;

        private string m_levelName;
        public string LevelName { get { return m_levelName; } }

        private BaseMapTile m_rootMapTile;
        public BaseMapTile RootMapTile { get { return m_rootMapTile; } }

        private BaseUnit m_levelSelectionUnit;
        private BaseUnit LevelSelectionUnit
        {
            get
            {
                // Lazily get and store level selection unit.
                return m_levelSelectionUnit ?? 
                       (m_levelSelectionUnit = ControllerContainer.BattleStateController.RegisteredTeams[TeamColor.Blue][0]);
            }
        }

        private Vector3 m_centeredCameraPosition;

        /// <summary>
        /// Sets the name of the level this selector should start.
        /// </summary>
        /// <param name="levelName">Name of the level.</param>
        /// <param name="orderNumber">The order number of this level selector.</param>
        /// <param name="rootMapTile">The maptile this levelselector lives on.</param>
        /// <param name="centeredCameraPosition">The position of the camera when this level selector is in the view center.</param>
        public void Initialize(string levelName, int orderNumber, BaseMapTile rootMapTile, Vector3 centeredCameraPosition)
        {
            m_levelName = levelName;
            m_rootMapTile = rootMapTile;
            m_centeredCameraPosition = centeredCameraPosition;

            // Color flag blue when level was completed; red otherwise
            m_levelFlagMeshRenderer.material = ControllerContainer.PlayerProgressionService.IsLevelCompleted(levelName)
                ? m_levelCompleteMaterial : m_levelNotCompleteMaterial;

            ControllerContainer.LevelSelectionInitializationController.RegisterLevelSelector(orderNumber, this);
        }

        /// <summary>
        /// Called when this LevelSelector was selected.
        /// </summary>
        public void OnSelected()
        {
            Debug.Log(string.Format("Selected LevelSelector representing level: {0}", m_levelName));

            // There is always only one unit in the level selection.
            BaseUnit levelSelectionUnit = LevelSelectionUnit;

            if (LevelSelectionUnit.CurrentSimplifiedPosition == m_rootMapTile.m_SimplifiedMapPosition)
            {
                //TODO: Enter level.
                Debug.Log(string.Format("Entering level: {0}", m_levelName));
                ControllerContainer.InputBlocker.ChangeBattleControlInput(true);

                SwitchToLevel();

                return;
            }

            Debug.Log(string.Format("Moving to level selector of level: {0}", m_levelName));

            IMovementCostResolver movementCostResolver = new LevelSelectionMovementCostResolver();
            Dictionary<Vector2, PathfindingNodeDebugData> dontCare;

            var routeToLevelSelector = ControllerContainer.TileNavigationController.GetBestWayToDestination(
                levelSelectionUnit.CurrentSimplifiedPosition, m_rootMapTile.m_SimplifiedMapPosition,
                movementCostResolver, out dontCare);

            ControllerContainer.InputBlocker.ChangeBattleControlInput(true, InputBlockMode.SelectionOnly);
            //TODO: hide opened level info.
            //TODO: Inject additional action to get maptile type updates while moving to switch visual of unit.
            levelSelectionUnit.MoveAlongRoute(routeToLevelSelector, movementCostResolver, tile =>
            {
                UpdateUnitVisuals(tile.MapTileType);

            }, () =>
            {
                ControllerContainer.InputBlocker.ChangeBattleControlInput(false, InputBlockMode.SelectionOnly);
                //TODO: display level info.
            });
        }

        /// <summary>
        /// Updates the visuals of level selection unit, based on the given <see cref="MapTileType"/>.
        /// </summary>
        /// <param name="mapTileType">Type of the map tile.</param>
        private void UpdateUnitVisuals(MapTileType mapTileType)
        {
            switch (mapTileType)
            {
                case MapTileType.Grass:
                case MapTileType.Forest:
                    LevelSelectionUnit.ChangeVisualsTo(UnitType.BattleTank);
                    break;
                case MapTileType.Water:
                    LevelSelectionUnit.ChangeVisualsTo(UnitType.WarShip);
                    break;
                case MapTileType.Mountain:
                    LevelSelectionUnit.ChangeVisualsTo(UnitType.Bomber);
                    break;
            }
        }

        /// <summary>
        /// Draws a map marker route to the given level selector.
        /// It's assumed that a connection exists.
        /// </summary>
        /// <param name="levelSelector">The level selector to draw a route to.</param>
        /// <param name="isLastRoute">Determines if this drawn route is the last unlocked route leading to the newest level.</param>
        public void DrawRouteToLevelSelector(LevelSelector levelSelector, bool isLastRoute)
        {
            var navigationController = ControllerContainer.TileNavigationController;
            Dictionary<Vector2, PathfindingNodeDebugData> dontCare;

            var routeToLevelSelector = navigationController.GetBestWayToDestination(
                RootMapTile.m_SimplifiedMapPosition, levelSelector.RootMapTile.m_SimplifiedMapPosition,
                new LevelSelectionMovementCostResolver(), out dontCare);

            var routeMarkerDefinitions = navigationController.GetRouteMarkerDefinitions(routeToLevelSelector);

            List<GameObject> routeGameObjects = new List<GameObject>();
            foreach (var routeMarkerDefinition in routeMarkerDefinitions)
            {
                GameObject instantiatedRoute = navigationController.GetMapTile(routeMarkerDefinition.Key).InstantiateLevelSelectionRoute(routeMarkerDefinition.Value);

                if (instantiatedRoute != null)
                {
                    routeGameObjects.Add(instantiatedRoute);
                }
            }

            if (isLastRoute && !ControllerContainer.PlayerProgressionService.LastUnlockedLevel.Equals(levelSelector.LevelName))
            {
                StartCoroutine(ShowLevelSelectionRoute(routeGameObjects, levelSelector, 0.3f));
                ControllerContainer.PlayerProgressionService.LastUnlockedLevel = levelSelector.LevelName;
            }
            else
            {
                foreach (var routeGameObject in routeGameObjects)
                {
                    routeGameObject.SetActive(true);
                }

                levelSelector.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Displays an instantiated route to a level selector.
        /// </summary>
        /// <param name="routeGameObjects">The route objects to show.</param>
        /// <param name="targetLevelSelector">The level selector the route leads to.</param>
        /// <param name="displayDelay">The delay inbetween display an object of the route.</param>
        private IEnumerator ShowLevelSelectionRoute(List<GameObject> routeGameObjects, LevelSelector targetLevelSelector, float displayDelay)
        {
            foreach (var routeGameObject in routeGameObjects)
            {
                yield return new WaitForSeconds(displayDelay);
                routeGameObject.SetActive(true);
            }
        
            yield return new WaitForSeconds(displayDelay);
            targetLevelSelector.gameObject.SetActive(true);
        }

        /// <summary>
        /// Checks the level played level. When this level selector is representing that level, 
        /// it'll position the level selection unit on it.
        /// </summary>
        public void ValidateLevelSelectionUnitsPosition()
        {
            if (m_levelName.Equals(ControllerContainer.PlayerProgressionService.LastPlayedLevel))
            {
                LevelSelectionUnit.SetPositionTo(m_rootMapTile);
                UpdateUnitVisuals(m_rootMapTile.MapTileType);

                ControllerContainer.MonoBehaviourRegistry.Get<CameraControls>().SetCameraPositionTo(m_centeredCameraPosition);
            }
        }

        /// <summary>
        /// Switches to the level of this selector.
        /// </summary>
        private void SwitchToLevel()
        {
            Root.Instance.LoadingUi.Show();

            Root.Instance.CoroutineHelper.CallDelayed(Root.Instance, 1.05f, () =>
            {
                Root.Instance.SceneLoading.UnloadExistingScenes(() =>
                {
                    Root.Instance.SceneLoading.LoadToLevel(m_levelName, () =>
                    {
                        ControllerContainer.PlayerProgressionService.LastPlayedLevel = m_levelName;
                        ControllerContainer.InputBlocker.ChangeBattleControlInput(false);
                        Root.Instance.LoadingUi.Hide();
                    });
                });
            });
        }
    }
}