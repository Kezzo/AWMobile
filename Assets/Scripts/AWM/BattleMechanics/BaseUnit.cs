using System;
using System.Collections;
using System.Collections.Generic;
using AWM.BattleVisuals;
using AWM.Enums;
using AWM.MapTileGeneration;
using AWM.Models;
using AWM.System;
using AWM.UI;
using UnityEngine;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// Class to handle to control a unit an handle the display and behaviors of a unit.
    /// </summary>
    public class BaseUnit : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_selectionMarker;

        [SerializeField]
        private GameObject m_attackMarker;

        [SerializeField]
        private float m_worldMovementSpeed;

        [SerializeField]
        private MeshFilter m_meshFilter;

        [SerializeField]
        private MeshRenderer m_meshRenderer;

        [SerializeField]
        private UnitStatManagement m_statManagement;
        public UnitStatManagement StatManagement { get { return m_statManagement; } }

        [SerializeField]
        private UnitParticleFxPlayer m_unitParticleFxPlayer;

        [SerializeField]
        private float m_disabledBrightness;

        [SerializeField]
        private AnimationCurve m_movementAnimationCurve;

        public TeamColor TeamColor { get; private set; }
        public UnitType UnitType { get; private set; }
        public bool UnitHasMovedThisRound { get; private set; }

        public int UniqueIdent { get; private set; }

        private bool m_unitHasAttackedThisRound;
        public bool UnitHasAttackedThisRound
        {
            get
            {
                return m_unitHasAttackedThisRound;
            }
            private set
            {
                if (m_materialPropertyBlock == null)
                {
                    m_materialPropertyBlock = new MaterialPropertyBlock();
                }

                m_unitHasAttackedThisRound = value;

                m_materialPropertyBlock.SetFloat("_Brightness", value ? m_disabledBrightness : 1f);
                m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);

                if (m_unitHasAttackedThisRound)
                {
                    ControllerContainer.BattleStateController.OnUnitIsDoneThisTurn(UniqueIdent, TeamColor);
                }
            }
        }

        private MaterialPropertyBlock m_materialPropertyBlock;

        private Vector2 m_currentSimplifiedPosition;
        public Vector2 CurrentSimplifiedPosition { get { return m_currentSimplifiedPosition; } }

        private List<BaseMapTile> m_currentWalkableMapTiles;
        private List<BaseMapTile> m_currentAttackableMapTiles;

        private List<BaseUnit> m_attackableUnits = new List<BaseUnit>();

        private Vector2 m_initialSimplifiedPosition;

        /// <summary>
        /// Initializes the specified team.
        /// </summary>
        /// <param name="unitData">The unit data.</param>
        /// <param name="unitMesh">The unit mesh.</param>
        /// <param name="initialSimplifiedPosition">The initial simplified position.</param>
        /// <param name="registerUnit">if set to <c>true</c> [register unit].</param>
        public void Initialize(MapGenerationData.Unit unitData, Mesh unitMesh, Vector2 initialSimplifiedPosition, bool registerUnit)
        {
            TeamColor = unitData.m_TeamColor;
            UnitType = unitData.m_UnitType;
            UnitHasMovedThisRound = false;
            UnitHasAttackedThisRound = false;

            m_meshFilter.mesh = unitMesh;
            m_initialSimplifiedPosition = initialSimplifiedPosition;
            m_currentSimplifiedPosition = initialSimplifiedPosition;

            if (!Application.isPlaying || !registerUnit)
            {
                return;
            }

            UniqueIdent = ControllerContainer.BattleStateController.RegisterUnit(TeamColor, this);
            m_statManagement.Initialize(this, GetUnitBalancing().m_Health);

            if (!Root.Instance.SceneLoading.IsInLevelSelection && ControllerContainer.BattleStateController.IsTeamWithColorPlayersTeam(unitData.m_TeamColor))
            {
                ControllerContainer.BattleStateController.OnTurnStartListener.Add(UniqueIdent.ToString(), OnTurnStarted);
                ControllerContainer.BattleStateController.OnUnitSelectedListener.Add(UniqueIdent, OnUnitOfPlayerTeamChangedSelection);
            }
        }

        /// <summary>
        /// Kills this unit.
        /// </summary>
        public void Die()
        {
            ControllerContainer.BattleStateController.RemoveRegisteredUnit(TeamColor, this);
            ControllerContainer.BattleStateController.OnUnitSelectedListener.Remove(UniqueIdent);
            ControllerContainer.BattleStateController.OnTurnStartListener.Remove(UniqueIdent.ToString());

            m_attackMarker.SetActive(false);
            m_unitParticleFxPlayer.PlayPfx(UnitParticleFx.Death);
            m_meshRenderer.enabled = false;

            Root.Instance.CoroutineHelper.CallDelayed(this, 0.6f, () =>
            {
                UpdateEnvironmentVisibility(CurrentSimplifiedPosition, false);
                Destroy(this.gameObject);
            });
        }

        /// <summary>
        /// Attacks the unit.
        /// </summary>
        /// <param name="baseUnit">The base unit.</param>
        /// <param name="onBattleSequenceFinished">The on battle sequence finished.</param>
        public void AttackUnit(BaseUnit baseUnit, Action onBattleSequenceFinished = null)
        {
            Vector2 unitPositionDiff = baseUnit.CurrentSimplifiedPosition - CurrentSimplifiedPosition;

            CardinalDirection directionToRotateTo = ControllerContainer.TileNavigationController.
                GetCardinalDirectionFromNodePositionDiff(unitPositionDiff);

            SetRotation(directionToRotateTo);

            baseUnit.ChangeVisibilityOfAttackMarker(false);
            baseUnit.StatManagement.HidePotentialDamage();

            baseUnit.StatManagement.TakeDamage(GetDamageOnUnit(baseUnit));
            m_unitParticleFxPlayer.PlayPfx(UnitParticleFx.Attack);

            UnitHasAttackedThisRound = true;
            // An attack will always keep the unit from moving in this round.
            UnitHasMovedThisRound = true;

            if (onBattleSequenceFinished != null)
            {
                onBattleSequenceFinished();
            }

            ControllerContainer.BattleStateController.OnBattleDone();
        }

        /// <summary>
        /// Gets the damage this unit does on a specific unit.
        /// </summary>
        /// <param name="baseUnit">The base unit.</param>
        /// <returns></returns>
        public int GetDamageOnUnit(BaseUnit baseUnit)
        {
            return GetUnitBalancing().GetDamageOnUnitType(baseUnit.UnitType);
        }

        /// <summary>
        /// Determines whether this instance can counter attack a specified unit.
        /// </summary>
        /// <param name="unitToCounterAttack">The unit to counter attack.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can counter attack] the specified unit to counter attack; otherwise, <c>false</c>.
        /// </returns>
        public bool CanCounterAttack(BaseUnit unitToCounterAttack)
        {
            return !StatManagement.IsDead && 
                   CanAttackUnit(unitToCounterAttack) && 
                   ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                       this.CurrentSimplifiedPosition, unitToCounterAttack.CurrentSimplifiedPosition) == 1;
        }

        /// <summary>
        /// Prepares and shows the battle sequence against a given unit.
        /// </summary>
        /// <param name="attackedUnit">The base unit.</param>
        /// <param name="damageToAttackedUnit">The damage to attacked unit.</param>
        /// <param name="onBattleSequenceFinished">The on battle sequence finished.</param>
        private void PrepareAndShowBattleSequence(BaseUnit attackedUnit, int damageToAttackedUnit, Action onBattleSequenceFinished)
        {
            BattleUI battleUi;

            if (ControllerContainer.MonoBehaviourRegistry.TryGet(out battleUi))
            {
                MapGenerationData.MapTile attackingUnitData = GetMapTileDataFromUnit(this);
                MapGenerationData.MapTile attackedUnitData = GetMapTileDataFromUnit(attackedUnit);

                battleUi.ShowBattleSequence(attackingUnitData, this.StatManagement.CurrentHealth,
                    attackedUnitData, attackedUnit.StatManagement.CurrentHealth, damageToAttackedUnit, onBattleSequenceFinished);
            }
        }

        /// <summary>
        /// Gets the map tile data from unit.
        /// </summary>
        /// <param name="baseUnit">The base unit.</param>
        /// <returns></returns>
        private MapGenerationData.MapTile GetMapTileDataFromUnit(BaseUnit baseUnit)
        {
            return new MapGenerationData.MapTile
            {
                m_PositionVector = CurrentSimplifiedPosition,

                m_MapTileType = ControllerContainer.TileNavigationController.GetMapTile(
                    baseUnit.CurrentSimplifiedPosition).MapTileType,

                m_Unit = new MapGenerationData.Unit
                {
                    m_TeamColor = baseUnit.TeamColor,
                    m_UnitType = baseUnit.UnitType
                }
            };
        }

        /// <summary>
        /// Sets the team color material.
        /// </summary>
        /// <param name="material">The material.</param>
        public void SetTeamColorMaterial(Material material)
        {
            m_meshRenderer.material = material;
        }

        /// <summary>
        /// Changes the unit mesh based on the unit type.
        /// </summary>
        /// <param name="unitType">Type of the unit.</param>
        public void ChangeVisualsTo(UnitType unitType)
        {
            if (unitType == UnitType)
            {
                // Already showing correct mesh.
                return;
            }

            MapTileGeneratorEditor mapTileGeneratorEditor;
            if (!ControllerContainer.MonoBehaviourRegistry.TryGet(out mapTileGeneratorEditor))
            {
                return;
            }

            m_meshFilter.mesh = mapTileGeneratorEditor.GetMeshOfUnitType(unitType);
            m_meshRenderer.material = mapTileGeneratorEditor.GetMaterialForTeamColor(unitType, TeamColor);

            Debug.Log(string.Format("Changed unit visuals from '{0}' to '{1}'", UnitType, unitType));

            UnitType = unitType;
        }

        /// <summary>
        /// Resets the unit.
        /// </summary>
        public void ResetUnit()
        {
            UnitHasMovedThisRound = false;
            UnitHasAttackedThisRound = false;
        }

        /// <summary>
        /// Called when this unit was selected.
        /// Will call the MovementService to get the positions the unit can move to
        /// </summary>
        public void OnUnitWasSelected()
        {
            Debug.LogFormat("Unit: '{0}' from Team: '{1}' was selected.", UnitType, TeamColor);

            m_selectionMarker.SetActive(true);

            if (!UnitHasMovedThisRound)
            {
                m_currentWalkableMapTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(
                    this.CurrentSimplifiedPosition, new UnitBalancingMovementCostResolver(GetUnitBalancing()));
                SetWalkableTileFieldVisibilityTo(true);
            }

            if (!UnitHasAttackedThisRound)
            {
                TryToDisplayActionOnUnitsInRange(out m_attackableUnits);
            }

            HideAttackRange();
            DislayAttackRange(CurrentSimplifiedPosition);

            ControllerContainer.BattleStateController.OnUnitChangedSelection(true);
        }

        /// <summary>
        /// Called when the unit was deselected.
        /// </summary>
        public void OnUnitWasDeselected()
        {
            m_selectionMarker.SetActive(false);
            ChangeVisibilityOfAttackMarker(false);

            foreach (var attackableUnit in m_attackableUnits)
            {
                attackableUnit.StatManagement.HidePotentialDamage();
            }
        
            SetWalkableTileFieldVisibilityTo(false);
            HideAllRouteMarker();

            m_currentWalkableMapTiles = null;

            ClearAttackableUnits(m_attackableUnits);

            HideAttackRange();

            ControllerContainer.BattleStateController.OnUnitChangedSelection(false);
        }

        /// <summary>
        /// Called when a unit of the player's team was selected or deselected.
        /// </summary>
        /// <param name="wasSelected">if set to <c>true</c> a unit was selected; otherwise false.</param>
        private void OnUnitOfPlayerTeamChangedSelection(bool wasSelected)
        {
            if (!CanUnitTakeAction())
            {
                return;
            }

            ChangeBlinkOrchestratorAffiliation(!wasSelected);
        }

        /// <summary>
        /// Invoked when a turn started.
        /// </summary>
        /// <param name="teamThatIsPlayingTheTurn">The team that is playing the turn.</param>
        private void OnTurnStarted(Team teamThatIsPlayingTheTurn)
        {
            ChangeBlinkOrchestratorAffiliation(ControllerContainer.BattleStateController.GetCurrentlyPlayingTeam().m_TeamColor == TeamColor);
        }

        /// <summary>
        /// Changes the blink orchestrator affiliation of this unit
        /// </summary>
        /// <param name="addToOrchestrator">if set to <c>true</c> the renderer of this unit will be added to the orchestrator; otherwise it'll be removed.</param>
        private void ChangeBlinkOrchestratorAffiliation(bool addToOrchestrator)
        {
            ShaderBlinkOrchestrator shaderBlinkOrchestrator = null;

            if (ControllerContainer.MonoBehaviourRegistry.TryGet(out shaderBlinkOrchestrator))
            {
                if (addToOrchestrator)
                {
                    shaderBlinkOrchestrator.AddRendererToBlink(ShaderCategory.Unit,
                        m_initialSimplifiedPosition, m_meshRenderer);
                }
                else
                {
                    shaderBlinkOrchestrator.RemoveRenderer(ShaderCategory.Unit, m_initialSimplifiedPosition);
                }

            }
        }

        /// <summary>
        /// Changes the visibility of attack marker.
        /// </summary>
        /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
        private void ChangeVisibilityOfAttackMarker(bool setVisible)
        {
            m_attackMarker.SetActive(setVisible);
        }

        /// <summary>
        /// Updates the visibility of the environment of a maptile that is searched based on the given position.
        /// </summary>
        /// <param name="positionOfTileToUpdate">The position of tile to update.</param>
        /// <param name="unitIsOnTile">if set to <c>true</c> a unit is on the tile.</param>
        private void UpdateEnvironmentVisibility(Vector2 positionOfTileToUpdate, bool unitIsOnTile)
        {
            BaseMapTile destinationMapTile = ControllerContainer.TileNavigationController
                .GetMapTile(positionOfTileToUpdate);

            if (destinationMapTile != null)
            {
                destinationMapTile.UpdateVisibilityOfEnvironment(unitIsOnTile);
            }
        }

        /// <summary>
        /// Changes the visibility of attack range marker.
        /// </summary>
        /// <param name="sourceNode">The source node to check the attack range from.</param>
        public void DislayAttackRange(Vector2 sourceNode)
        {
            List<BaseMapTile> mapTileInAttackRange = ControllerContainer.TileNavigationController.
                GetMapTilesInRange(sourceNode, GetUnitBalancing().m_AttackRange);

            for (int i = 0; i < mapTileInAttackRange.Count; i++)
            {
                mapTileInAttackRange[i].DisplayAttackRangeMarker(mapTileInAttackRange, sourceNode);
            }

            m_currentAttackableMapTiles = mapTileInAttackRange;
        }

        /// <summary>
        /// Hides the currently displayed attack range marker.
        /// </summary>
        public void HideAttackRange()
        {
            if (m_currentAttackableMapTiles == null || m_currentAttackableMapTiles.Count == 0)
            {
                return;
            }

            for (int i = 0; i < m_currentAttackableMapTiles.Count; i++)
            {
                m_currentAttackableMapTiles[i].HideAttackRangeMarker();
            }
        }

        /// <summary>
        /// Hides all route marker.
        /// </summary>
        private void HideAllRouteMarker()
        {
            if (m_currentWalkableMapTiles == null)
            {
                //Debug.LogError("Redundant call of HideAllRouteMarker.");
                return;
            }

            for (int tileIndex = 0; tileIndex < m_currentWalkableMapTiles.Count; tileIndex++)
            {
                m_currentWalkableMapTiles[tileIndex].HideAllRouteMarker();
            }
        }

        /// <summary>
        /// Sets the walkable tile field visibility to.
        /// </summary>
        /// <param name="setVisibilityTo">if set to <c>true</c> [set visibility to].</param>
        private void SetWalkableTileFieldVisibilityTo(bool setVisibilityTo)
        {
            if (m_currentWalkableMapTiles == null)
            {
                //Debug.LogError("Redundant call of SetWalkableTileFieldVisibilityTo.");
                return;
            }

            for (int tileIndex = 0; tileIndex < m_currentWalkableMapTiles.Count; tileIndex++)
            {
                m_currentWalkableMapTiles[tileIndex].ChangeVisibilityOfMovementField(setVisibilityTo);
            }
        }

        /// <summary>
        /// Determines whether this instance can be selected.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can be selected; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUnitTakeAction()
        {
            return (!UnitHasMovedThisRound || !UnitHasAttackedThisRound) && ControllerContainer.BattleStateController.GetCurrentlyPlayingTeam().m_TeamColor == TeamColor;
        }

        /// <summary>
        /// Gets the unit balancing.
        /// </summary>
        /// <returns></returns>
        public UnitBalancingData GetUnitBalancing()
        {
            return ControllerContainer.UnitBalancingProvider.GetUnitBalancing(UnitType);
        }

        /// <summary>
        /// Determines whether this unit can attack a given unit.
        /// </summary>
        /// <param name="unitToCheck">The unit to check.</param>
        public bool CanAttackUnit(BaseUnit unitToCheck)
        {
            return TeamColor != unitToCheck.TeamColor && 
                   GetUnitBalancing().m_AttackableUnitMetaTypes.Contains(unitToCheck.GetUnitBalancing().m_UnitMetaType) &&
                   GetDamageOnUnit(unitToCheck) > 0;
        }

        /// <summary>
        /// Displays the route to the destination.
        /// </summary>
        /// <param name="routeToDestination">The route to destination.</param>
        /// <param name="onUnitMovedToDestinationCallback">The on unit moved to destination callback.</param>
        public void DisplayRouteToDestination(List<Vector2> routeToDestination, Action<int> onUnitMovedToDestinationCallback)
        {
            HideAllRouteMarker();
            SetWalkableTileFieldVisibilityTo(true);

            HideAttackRange();
            DislayAttackRange(routeToDestination[routeToDestination.Count - 1]);

            var routeMarkerDefinitions = ControllerContainer.TileNavigationController.GetRouteMarkerDefinitions(routeToDestination);

            for (int routeMarkerIndex = 0; routeMarkerIndex < routeMarkerDefinitions.Count; routeMarkerIndex++)
            {
                var routeMarkerDefinition = routeMarkerDefinitions[routeMarkerIndex];

                BaseMapTile mapTile = ControllerContainer.TileNavigationController.GetMapTile(routeMarkerDefinition.Key);

                if (mapTile != null)
                {
                    mapTile.DisplayRouteMarker(routeMarkerDefinition.Value);
                }
            }

            ControllerContainer.BattleStateController.AddConfirmMoveButtonPressedListener(() =>
            {
                ControllerContainer.BattleStateController.RemoveCurrentConfirmMoveButtonPressedListener();

                SetWalkableTileFieldVisibilityTo(false);
                ClearAttackableUnits(m_attackableUnits);
                UnitHasMovedThisRound = true;

                MoveAlongRoute(routeToDestination, new UnitBalancingMovementCostResolver(
                    GetUnitBalancing()), null, () =>
                {
                    if (!UnitHasMovedThisRound)
                    {
                        // turn was ended before unit reached it's destination

                        if (onUnitMovedToDestinationCallback != null)
                        {
                            onUnitMovedToDestinationCallback(UniqueIdent);
                        }

                        return;
                    }

                    if (TryToDisplayActionOnUnitsInRange(out m_attackableUnits))
                    {
                        Debug.Log("Attackable units: "+m_attackableUnits.Count);

                        HideAllRouteMarker();
                    }
                    else
                    {
                        UnitHasAttackedThisRound = true;

                        if (onUnitMovedToDestinationCallback != null)
                        {
                            onUnitMovedToDestinationCallback(UniqueIdent);
                        }
                    }
                
                });
            });
        }

        /// <summary>
        /// Clears the action on units.
        /// </summary>
        /// <param name="unitToClearActionsFrom">The unit to clear actions from.</param>
        private void ClearAttackableUnits(List<BaseUnit> unitToClearActionsFrom)
        {
            if (unitToClearActionsFrom == null)
            {
                return;
            }

            for (int unitIndex = 0; unitIndex < unitToClearActionsFrom.Count; unitIndex++)
            {
                unitToClearActionsFrom[unitIndex].ChangeVisibilityOfAttackMarker(false);
                unitToClearActionsFrom[unitIndex].StatManagement.HidePotentialDamage();
            }

            unitToClearActionsFrom.Clear();
        }

        /// <summary>
        /// Tries to display action on units in range.
        /// For units that can take action on friendly units, it will display the field to do the friendly action on the unit.
        /// For enemy units the attack field will be displayed, if the unit can attack.
        /// </summary>
        /// <returns></returns>
        private bool TryToDisplayActionOnUnitsInRange(out List<BaseUnit> attackableUnits)
        {
            List<BaseUnit> unitsInRange = ControllerContainer.BattleStateController.GetUnitsInRange(
                this.CurrentSimplifiedPosition, GetUnitBalancing().m_AttackRange);

            attackableUnits = new List<BaseUnit>();

            for (int unitIndex = 0; unitIndex < unitsInRange.Count; unitIndex++)
            {
                BaseUnit unit = unitsInRange[unitIndex];

                if (CanAttackUnit(unit))
                {
                    unit.ChangeVisibilityOfAttackMarker(true);
                    unit.StatManagement.DisplayPotentialDamage(this);
                    attackableUnits.Add(unit);
                }
                else
                {
                    //TODO: Handle interaction with friendly units.
                }
            }

            return attackableUnits.Count > 0;
        }

        /// <summary>
        /// Sets the position of this unit to the given <see cref="BaseMapTile"/>.
        /// </summary>
        /// <param name="mapTile">The base map tile to position the unit on.</param>
        public void SetPositionTo(BaseMapTile mapTile)
        {
            this.transform.SetParent(mapTile.UnitRoot);
            this.transform.localPosition = Vector3.zero;
            m_currentSimplifiedPosition = mapTile.m_SimplifiedMapPosition;
        }

        /// <summary>
        /// Moves the along route.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="movementCostResolver">The implementation that determines how a unit is affect by the terrain to change the movement speed accordingly.</param>
        /// <param name="onReachedTile">Invoked when a tile was reached while moving.</param>
        /// <param name="onMoveFinished">The on move finished.</param>
        /// <returns></returns>
        private IEnumerator MoveAlongRouteCoroutine(List<Vector2> route, IMovementCostResolver movementCostResolver, 
            Action<BaseMapTile> onReachedTile, Action onMoveFinished)
        {
            // Starting with an index of 1 here, because the node at index 0 is the node the unit is standing on.
            for (int nodeIndex = 1; nodeIndex < route.Count; nodeIndex++)
            {
                Vector2 nodeToMoveTo = route[nodeIndex];
                Vector2 currentNode = route[nodeIndex - 1];

                yield return MoveToNeighborNode(currentNode, nodeToMoveTo, movementCostResolver,
                    nodeIndex == 1, nodeIndex == route.Count - 1, onReachedTile);

                if (nodeIndex == route.Count - 1)
                {
                    if (onMoveFinished != null)
                    {
                        onMoveFinished();
                    }
                }
            }
        }

        /// <summary>
        /// Moves the along route. This method will also instantly set the unit position to the destination node to avoid units standing on the same position.
        /// </summary>
        /// <param name="route">The route.</param>
        /// <param name="movementCostResolver">The implementation that determines how a unit is affect by the terrain to change the movement speed accordingly.</param>
        /// <param name="onReachedTile">Invoked when a tile was reached while moving.</param>
        /// <param name="onMoveFinished">Invoked when the unit was successfully moved.</param>
        public void MoveAlongRoute(List<Vector2> route, IMovementCostResolver movementCostResolver, 
            Action<BaseMapTile> onReachedTile, Action onMoveFinished)
        {
            UpdateEnvironmentVisibility(CurrentSimplifiedPosition, false);
            m_currentSimplifiedPosition = route[route.Count - 1];

            StartCoroutine(MoveAlongRouteCoroutine(route, movementCostResolver, onReachedTile, () =>
            {
                UpdateEnvironmentVisibility(route[route.Count - 1], true);
                onMoveFinished();
            }));
        }

        /// <summary>
        /// Moves from to neighbor node.
        /// </summary>
        /// <param name="startNode">The start node.</param>
        /// <param name="destinationNode">The destination node.</param>
        /// <param name="movementCostResolver">The implementation that determines how a unit is affect by the terrain to change the movement speed accordingly.</param>
        /// <param name="isMovingFromFirstTile">
        /// Determines if this method is moving from the first tile.
        /// Needed to properly evaluate movement animation curve the same for all distances.
        /// </param>
        /// <param name="isMovingToLastTile">
        /// Determines if this method is moving to the last tile.
        /// Needed to properly evaluate movement animation curve the same for all distances.
        /// </param>
        /// <param name="onReachedTile">Invoked when a tile was reached while moving.</param>
        public IEnumerator MoveToNeighborNode(Vector2 startNode, Vector2 destinationNode, IMovementCostResolver movementCostResolver,
            bool isMovingFromFirstTile, bool isMovingToLastTile, Action<BaseMapTile> onReachedTile)
        {
            Vector2 nodePositionDiff = destinationNode - startNode;

            // Rotate unit to destination node
            CardinalDirection directionToRotateTo = ControllerContainer.TileNavigationController.
                GetCardinalDirectionFromNodePositionDiff(nodePositionDiff);

            SetRotation(directionToRotateTo);

            BaseMapTile startMapTile = ControllerContainer.TileNavigationController.GetMapTile(startNode);
            BaseMapTile destinationMapTile = ControllerContainer.TileNavigationController.GetMapTile(destinationNode);

            Vector3 targetWorldPosition = Vector3.zero;

            if (destinationMapTile != null)
            {
                targetWorldPosition = destinationMapTile.UnitRoot.position;
                this.transform.SetParent(destinationMapTile.UnitRoot, true);
            }
            else
            {
                Debug.LogErrorFormat("Unable to find destination MapTile for node: '{0}'", destinationNode);
                yield break;
            }

            bool reachedMapTile = false;
            float distanceToNeighbourTile = (targetWorldPosition - transform.position).magnitude;
            float startDistanceToNeighbourTile = distanceToNeighbourTile;

            // Move to world position
            while (true)
            {
                float animationCurveValue = 0.5f;

                if (isMovingFromFirstTile)
                {
                    animationCurveValue = 0.5f - (distanceToNeighbourTile / startDistanceToNeighbourTile) / 2;
                }

                if (isMovingToLastTile)
                {
                    animationCurveValue = 1f - (distanceToNeighbourTile / startDistanceToNeighbourTile) / 2;
                }

                float terrainSpeedModifier = movementCostResolver.GetMovementCostToWalkOnMapTileType(distanceToNeighbourTile <= 1f ? 
                    destinationMapTile.MapTileType : startMapTile.MapTileType) > 1 ? 0.75f : 1f;

                float movementStep = m_worldMovementSpeed * terrainSpeedModifier * m_movementAnimationCurve.Evaluate(animationCurveValue) * Time.deltaTime;

                transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, movementStep);

                distanceToNeighbourTile = (targetWorldPosition - transform.position).magnitude;

                if (!reachedMapTile && distanceToNeighbourTile <= 1f)
                {
                    reachedMapTile = true;

                    if (onReachedTile != null)
                    {
                        onReachedTile(destinationMapTile);
                    }
                }

                if (transform.position == targetWorldPosition)
                {
                    yield break;
                }

                while (ControllerContainer.BattleStateController.IsBattlePaused)
                {
                    yield return null;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Sets the rotation of the baseunit.
        /// </summary>
        /// <param name="directionToRotateTo">The direction to rotate to.</param>
        public void SetRotation(CardinalDirection directionToRotateTo)
        {
            switch (directionToRotateTo)
            {
                case CardinalDirection.North:
                    this.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    break;
                case CardinalDirection.East:
                    this.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                    break;
                case CardinalDirection.South:
                    this.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    break;
                case CardinalDirection.West:
                    this.transform.rotation = Quaternion.Euler(0f, 270f, 0f);
                    break;
            }
        }
    }
}
