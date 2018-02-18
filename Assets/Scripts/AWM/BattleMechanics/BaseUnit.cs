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

        public UnitParticleFxPlayer UnitParticleFxPlayer { get; private set; }

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
            set
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
                    CC.BSC.OnUnitIsDoneThisTurn(UniqueIdent, TeamColor);
                }
            }
        }

        private MaterialPropertyBlock m_materialPropertyBlock;

        private Vector2 m_currentSimplifiedPosition;
        public Vector2 CurrentSimplifiedPosition { get { return m_currentSimplifiedPosition; } }

        private List<KeyValuePair<Vector2, RouteMarkerDefinition>> m_currentlyDisplayedRouteMarker;
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

            UniqueIdent = CC.BSC.RegisterUnit(TeamColor, this);
            m_statManagement.Initialize(this, GetUnitBalancing().m_Health);

            if (!Root.Instance.SceneLoading.IsInLevelSelection)
            {
                if (CC.BSC.IsTeamWithColorPlayersTeam(unitData.m_TeamColor))
                {
                    CC.BSC.OnTurnStartListener.Add(UniqueIdent.ToString(), OnTurnStarted);
                    CC.BSC.OnUnitSelectedListener.Add(UniqueIdent, OnUnitOfPlayerTeamChangedSelection);
                }
                
                UnitParticleFxPlayer = CC.MBR.Get<UnitParticleFxPlayer>();
            }
        }

        /// <summary>
        /// Kills this unit.
        /// </summary>
        public void Die()
        {
            CC.BSC.RemoveRegisteredUnit(TeamColor, this);
            CC.BSC.OnUnitSelectedListener.Remove(UniqueIdent);
            CC.BSC.OnTurnStartListener.Remove(UniqueIdent.ToString());

            m_attackMarker.SetActive(false);
            UnitParticleFxPlayer.PlayPfxAt(UnitParticleFx.Death, this.gameObject.transform.position);
            m_meshRenderer.enabled = false;

            UpdateEnvironmentVisibility(CurrentSimplifiedPosition, false);
            Destroy(this.gameObject);
        }

        /// <summary>
        /// Attacks the unit.
        /// </summary>
        /// <param name="baseUnit">The base unit.</param>
        /// <param name="onBattleSequenceFinished">The on battle sequence finished.</param>
        public void AttackUnit(BaseUnit baseUnit, Action onBattleSequenceFinished = null)
        {
            Vector2 unitPositionDiff = baseUnit.CurrentSimplifiedPosition - CurrentSimplifiedPosition;

            CardinalDirection directionToRotateTo = CC.TNC.
                GetCardinalDirectionFromNodePositionDiff(unitPositionDiff);

            SetRotation(directionToRotateTo);

            baseUnit.ChangeVisibilityOfAttackMarker(false);
            baseUnit.StatManagement.HidePotentialDamage();

            baseUnit.StatManagement.TakeDamage(GetDamageOnUnit(baseUnit));
            //UnitParticleFxPlayer.PlayPfxAt(UnitParticleFx.Attack, this.gameObject.transform.position);

            UnitHasAttackedThisRound = true;
            // An attack will always keep the unit from moving in this round.
            UnitHasMovedThisRound = true;

            if (onBattleSequenceFinished != null)
            {
                onBattleSequenceFinished();
            }

            CC.BSC.OnBattleDone();
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
                   CC.TNC.GetDistanceToCoordinate(
                       this.CurrentSimplifiedPosition, unitToCounterAttack.CurrentSimplifiedPosition) == 1;
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
            if (!CC.MBR.TryGet(out mapTileGeneratorEditor))
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
                m_currentWalkableMapTiles = CC.TNC.GetWalkableMapTiles(
                    this.CurrentSimplifiedPosition, new UnitBalancingMovementCostResolver(GetUnitBalancing()));
                SetWalkableTileFieldVisibilityTo(true);
            }

            if (!UnitHasAttackedThisRound)
            {
                TryToDisplayActionOnUnitsInRange(out m_attackableUnits);
            }

            HideAttackRange();
            DislayAttackRange(CurrentSimplifiedPosition);

            CC.BSC.OnUnitChangedSelection(true);
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

            CC.BSC.OnUnitChangedSelection(false);
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
            ChangeBlinkOrchestratorAffiliation(CC.BSC.GetCurrentlyPlayingTeam().m_TeamColor == TeamColor);
        }

        /// <summary>
        /// Changes the blink orchestrator affiliation of this unit
        /// </summary>
        /// <param name="addToOrchestrator">if set to <c>true</c> the renderer of this unit will be added to the orchestrator; otherwise it'll be removed.</param>
        private void ChangeBlinkOrchestratorAffiliation(bool addToOrchestrator)
        {
            ShaderBlinkOrchestrator shaderBlinkOrchestrator = null;

            if (CC.MBR.TryGet(out shaderBlinkOrchestrator))
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
            BaseMapTile destinationMapTile = CC.TNC
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
            List<BaseMapTile> mapTileInAttackRange = CC.TNC.GetMapTilesInRange(sourceNode, GetUnitBalancing().m_AttackRange, true);

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
            if (m_currentlyDisplayedRouteMarker == null || m_currentlyDisplayedRouteMarker.Count == 0)
            {
                //Debug.LogError("Redundant call of HideAllRouteMarker.");
                return;
            }

            foreach (var routeMarkerData in m_currentlyDisplayedRouteMarker)
            {
                CC.TNC.GetMapTile(routeMarkerData.Key).HideAllRouteMarker();
            }

            m_currentlyDisplayedRouteMarker.Clear();
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
            return (!UnitHasMovedThisRound || !UnitHasAttackedThisRound) && CC.BSC.GetCurrentlyPlayingTeam().m_TeamColor == TeamColor;
        }

        /// <summary>
        /// Gets the unit balancing.
        /// </summary>
        /// <returns></returns>
        public UnitBalancingData GetUnitBalancing()
        {
            return CC.UBP.GetUnitBalancing(UnitType);
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

            m_currentlyDisplayedRouteMarker = CC.TNC.GetRouteMarkerDefinitions(routeToDestination);

            for (int routeMarkerIndex = 0; routeMarkerIndex < m_currentlyDisplayedRouteMarker.Count; routeMarkerIndex++)
            {
                var routeMarkerDefinition = m_currentlyDisplayedRouteMarker[routeMarkerIndex];

                BaseMapTile mapTile = CC.TNC.GetMapTile(routeMarkerDefinition.Key);

                if (mapTile != null)
                {
                    mapTile.DisplayRouteMarker(routeMarkerDefinition.Value);
                }
            }

            CC.BSC.AddConfirmMoveButtonPressedListener(() =>
            {
                CC.BSC.RemoveCurrentConfirmMoveButtonPressedListener();

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
            List<BaseUnit> unitsInRange = CC.BSC.GetUnitsInRange(
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
            mapTile.UpdateVisibilityOfEnvironment(true);

            BaseMapTile previousMapTile = CC.TNC.GetMapTile(m_currentSimplifiedPosition);

            if (previousMapTile != null)
            {
                previousMapTile.UpdateVisibilityOfEnvironment(false);
            }

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

                IEnumerator bridgeOpeningCoroutine = null;

                if (IsBridgeOnNode(nodeToMoveTo, true, out bridgeOpeningCoroutine))
                {
                    yield return bridgeOpeningCoroutine;
                }

                yield return MoveToNeighborNode(currentNode, nodeToMoveTo, movementCostResolver,
                    nodeIndex == 1, nodeIndex == route.Count - 1, onReachedTile);

                if (IsBridgeOnNode(currentNode, false, out bridgeOpeningCoroutine))
                {
                    yield return bridgeOpeningCoroutine;
                }

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
        /// Checks if a bridge is on the given node.
        /// </summary>
        /// <param name="node">The node to check.</param>
        /// <param name="openBridge">if set to true, will open the bridge; will close the bridge otherwise.</param>
        /// <param name="bridgeOpeningCoroutine">
        /// The coroutine that will run as long as the bridge is opening or closing. 
        /// Can be waited for to do something when the bridge is fully closed/open.
        /// </param>
        private bool IsBridgeOnNode(Vector2 node, bool openBridge, out IEnumerator bridgeOpeningCoroutine)
        {
            if (this.GetUnitBalancing().m_UnitMetaType == UnitMetaType.Water)
            {
                BaseMapTile mapTileToMoveTo = CC.TNC.GetMapTile(node);

                if (mapTileToMoveTo.MapTileType == MapTileType.Water && mapTileToMoveTo.HasStreet)
                {
                    bridgeOpeningCoroutine = mapTileToMoveTo.ChangeBridgeOpeningState(openBridge);
                    return true;
                }
            }

            bridgeOpeningCoroutine = null;
            return false;
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
            if (route == null || route.Count == 0)
            {
                onMoveFinished();
                return;
            }

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
            CardinalDirection directionToRotateTo = CC.TNC.
                GetCardinalDirectionFromNodePositionDiff(nodePositionDiff);

            SetRotation(directionToRotateTo);

            BaseMapTile startMapTile = CC.TNC.GetMapTile(startNode);
            BaseMapTile destinationMapTile = CC.TNC.GetMapTile(destinationNode);

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

                while (CC.BSC.IsBattlePaused)
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
