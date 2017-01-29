using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    private Color m_disabledColor;

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

            m_materialPropertyBlock.SetColor("_Color", value ? m_disabledColor : Color.white);

            m_meshRenderer.SetPropertyBlock(m_materialPropertyBlock);
        }
    }

    private MaterialPropertyBlock m_materialPropertyBlock;

    private Vector2 m_currentSimplifiedPosition;
    public Vector2 CurrentSimplifiedPosition { get { return m_currentSimplifiedPosition; } }

    private List<BaseMapTile> m_currentWalkableMapTiles;
    private BattlegroundUI m_battlegroundUi;

    private List<BaseUnit> m_attackableUnits;

    private void Start()
    {
        ControllerContainer.MonoBehaviourRegistry.TryGet(out m_battlegroundUi);
    }

    /// <summary>
    /// Initializes the specified team.
    /// </summary>
    /// <param name="unitData">The unit data.</param>
    /// <param name="unitMesh">The unit mesh.</param>
    /// <param name="initialSimplifiedPosition">The initial simplified position.</param>
    public void Initialize(MapGenerationData.Unit unitData, Mesh unitMesh, Vector2 initialSimplifiedPosition)
    {
        TeamColor = unitData.m_TeamColor;
        UnitType = unitData.m_UnitType;
        UnitHasMovedThisRound = false;

        m_meshFilter.mesh = unitMesh;
        m_currentSimplifiedPosition = initialSimplifiedPosition;

        if (Application.isPlaying)
        {
            UniqueIdent = ControllerContainer.BattleController.RegisterUnit(TeamColor, this);
            m_statManagement.Initialize(this, GetUnitBalancing().m_Health);
        }

        // Load balancing once here and keep for the round.
    }

    /// <summary>
    /// Kills this unit.
    /// </summary>
    public void Die()
    {
        ControllerContainer.BattleController.RemoveRegisteredUnit(TeamColor, this);
        // Play explosion effect and destroy delayed.

        Destroy(this.gameObject);
    }

    /// <summary>
    /// Attacks the unit.
    /// </summary>
    /// <param name="baseUnit">The base unit.</param>
    public void AttackUnit(BaseUnit baseUnit)
    {
        baseUnit.StatManagement.TakeDamage(GetUnitBalancing().m_Damage);
        baseUnit.ChangeVisibiltyOfAttackMarker(false);

        UnitHasAttackedThisRound = true;
        // An attack will always keep the unit from moving in this round.
        UnitHasMovedThisRound = true;
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
            m_currentWalkableMapTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(this);
            SetWalkableTileFieldVisibiltyTo(true);
        }

        if (!UnitHasAttackedThisRound)
        {
            TryToDisplayActionOnUnitsInRange(out m_attackableUnits);
        }
    }

    /// <summary>
    /// Called when the unit was deselected.
    /// </summary>
    public void OnUnitWasDeselected()
    {
        m_selectionMarker.SetActive(false);
        ChangeVisibiltyOfAttackMarker(false);

        SetWalkableTileFieldVisibiltyTo(false);
        HideAllRouteMarker();

        m_currentWalkableMapTiles = null;

        ClearAttackableUnits(m_attackableUnits);
    }

    /// <summary>
    /// Changes the visibilty of attack marker.
    /// </summary>
    /// <param name="setVisible">if set to <c>true</c> [set visible].</param>
    private void ChangeVisibiltyOfAttackMarker(bool setVisible)
    {
        m_attackMarker.SetActive(setVisible);
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
    /// Sets the walkable tile field visibilty to.
    /// </summary>
    /// <param name="setVisibiltyTo">if set to <c>true</c> [set visibilty to].</param>
    private void SetWalkableTileFieldVisibiltyTo(bool setVisibiltyTo)
    {
        if (m_currentWalkableMapTiles == null)
        {
            //Debug.LogError("Redundant call of SetWalkableTileFieldVisibiltyTo.");
            return;
        }

        for (int tileIndex = 0; tileIndex < m_currentWalkableMapTiles.Count; tileIndex++)
        {
            m_currentWalkableMapTiles[tileIndex].ChangeVisibiltyOfMovementField(setVisibiltyTo);
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
        return (!UnitHasMovedThisRound || !UnitHasAttackedThisRound) && ControllerContainer.BattleController.GetCurrentlyPlayingTeam().m_TeamColor == TeamColor;
    }

    /// <summary>
    /// Gets the unit balancing.
    /// </summary>
    /// <returns></returns>
    public SimpleUnitBalancing.UnitBalancing GetUnitBalancing()
    {
        return Root.Instance.SimeSimpleUnitBalancing.GetUnitBalancing(UnitType);
    }

    /// <summary>
    /// Displays the route to the destination.
    /// </summary>
    /// <param name="routeToDestination">The route to destination.</param>
    /// <param name="onUnitMovedToDestinationCallback">The on unit moved to destination callback.</param>
    public void DisplayRouteToDestination(List<Vector2> routeToDestination, Action<int> onUnitMovedToDestinationCallback)
    {
        HideAllRouteMarker();
        SetWalkableTileFieldVisibiltyTo(true);

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

        ControllerContainer.BattleController.AddConfirmMoveButtonPressedListener(() =>
        {
            ControllerContainer.BattleController.RemoveCurrentConfirmMoveButtonPressedListener();

            SetWalkableTileFieldVisibiltyTo(false);
            ClearAttackableUnits(m_attackableUnits);

            MoveAlongRoute(routeToDestination, () =>
            {
                if (TryToDisplayActionOnUnitsInRange(out m_attackableUnits))
                {
                    Debug.Log("Attackable units: "+m_attackableUnits.Count);

                    HideAllRouteMarker();
                    m_battlegroundUi.ChangeVisibilityOfConfirmMoveButton(false);
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
        for (int unitIndex = 0; unitIndex < unitToClearActionsFrom.Count; unitIndex++)
        {
            unitToClearActionsFrom[unitIndex].ChangeVisibiltyOfAttackMarker(false);
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
        List<BaseUnit> unitsInRange =
            ControllerContainer.BattleController.GetUnitsInRange(this.CurrentSimplifiedPosition, GetUnitBalancing().m_AttackRange);

        attackableUnits = new List<BaseUnit>();

        for (int unitIndex = 0; unitIndex < unitsInRange.Count; unitIndex++)
        {
            BaseUnit unit = unitsInRange[unitIndex];

            // Is unit enemy or friend?
            if (unit.TeamColor != TeamColor)
            {
                if (GetUnitBalancing().m_AttackableUnitMetaTypes.Contains(unit.GetUnitBalancing().m_UnitMetaType))
                {
                    unit.ChangeVisibiltyOfAttackMarker(true);
                    attackableUnits.Add(unit);
                }
            }
            else
            {
                //TODO: Handle interaction with friendly units.
            }
        }

        return attackableUnits.Count > 0; // || supportableUnits.Count > 0;
    }

    /// <summary>
    /// Moves the along route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="onMoveFinished">The on move finished.</param>
    /// <returns></returns>
    private IEnumerator MoveAlongRouteCoroutine(List<Vector2> route, Action onMoveFinished)
    {
        // Starting with an index of 1 here, because the node at index 0 is the node the unit is standing on.
        for (int nodeIndex = 1; nodeIndex < route.Count; nodeIndex++)
        {
            Vector2 nodeToMoveTo = route[nodeIndex];
            Vector2 currentNode = route[nodeIndex - 1];

            yield return MoveToNeighborNode(currentNode, nodeToMoveTo);

            if (nodeIndex == route.Count - 1)
            {
                UnitHasMovedThisRound = true;

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
    /// <param name="onMoveFinished">The on move finished.</param>
    public void MoveAlongRoute(List<Vector2> route, Action onMoveFinished)
    {
        m_currentSimplifiedPosition = route[route.Count - 1];

        StartCoroutine(MoveAlongRouteCoroutine(route, onMoveFinished));
    }

    /// <summary>
    /// Moves from to neighbor node.
    /// </summary>
    /// <param name="startNode">The start node.</param>
    /// <param name="destinationNode">The destination node.</param>
    private IEnumerator MoveToNeighborNode(Vector2 startNode, Vector2 destinationNode)
    {
        Vector2 nodePositionDiff = startNode - destinationNode;

        // Rotate unit to destination node
        CardinalDirection directionToRotateTo = ControllerContainer.TileNavigationController.GetCardinalDirectionFromNodePositionDiff(
            nodePositionDiff, false);

        SetRotation(directionToRotateTo);

        BaseMapTile mapTile = ControllerContainer.TileNavigationController.GetMapTile(destinationNode);
        Vector3 targetWorldPosition = Vector3.zero;

        if (mapTile != null)
        {
            targetWorldPosition = mapTile.UnitRoot.position;
            this.transform.SetParent(mapTile.UnitRoot, true);
        }
        else
        {
            Debug.LogErrorFormat("Unable to find destination MapTile for node: '{0}'", destinationNode);
            yield break;
        }

        // Move to world position
        while (true)
        {
            float movementStep = m_worldMovementSpeed * Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, movementStep);

            if (transform.position == targetWorldPosition)
            {
                yield break;
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
