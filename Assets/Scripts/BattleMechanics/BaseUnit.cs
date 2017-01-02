using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUnit : MonoBehaviour
{
    [SerializeField]
    private GameObject m_selectionMarker;

    [SerializeField]
    private float m_worldMovementSpeed;

    public Team TeamAffinity { get; private set; }
    public UnitType UnitType { get; private set; }
    public bool UnitHasActedThisRound { get; private set; }

    private Vector2 m_currentSimplifiedPosition;
    public Vector2 CurrentSimplifiedPosition { get { return m_currentSimplifiedPosition; } }

    private List<BaseMapTile> m_currentWalkableMapTiles;

    /// <summary>
    /// Initializes the specified team.
    /// </summary>
    /// <param name="unitData">The unit data.</param>
    /// <param name="initialSimplifiedPosition">The initial simplified position.</param>
    public void Initialize(MapGenerationData.Unit unitData, Vector2 initialSimplifiedPosition)
    {
        TeamAffinity = unitData.m_Team;
        UnitType = unitData.m_UnitType;
        UnitHasActedThisRound = false;

        m_currentSimplifiedPosition = initialSimplifiedPosition;

        if (Application.isPlaying)
        {
            ControllerContainer.BattleController.RegisterUnit(this);
        }
        
        // Load balancing once here and keep for the round.
    }

    /// <summary>
    /// Resets the unit.
    /// </summary>
    public void ResetUnit()
    {
        UnitHasActedThisRound = false;
    }

    /// <summary>
    /// Called when this unit was selected.
    /// Will call the MovementService to get the positions the unit can move to
    /// </summary>
    public void OnUnitWasSelected()
    {
        Debug.LogFormat("Unit: '{0}' from Team: '{1}' was selected.", UnitType, TeamAffinity.m_TeamColor);

        m_selectionMarker.SetActive(true);

        m_currentWalkableMapTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(this);
        SetWalkableTileFieldVisibiltyTo(true);
    }

    /// <summary>
    /// Called when the unit was deselected.
    /// </summary>
    public void OnUnitWasDeselected()
    {
        m_selectionMarker.SetActive(false);

        SetWalkableTileFieldVisibiltyTo(false);
        HideAllRouteMarker();

        m_currentWalkableMapTiles = null;
    }

    /// <summary>
    /// Hides all route marker.
    /// </summary>
    private void HideAllRouteMarker()
    {
        if (m_currentWalkableMapTiles == null)
        {
            Debug.LogError("Redundant call of HideAllRouteMarker.");
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
            Debug.LogError("Redundant call of SetWalkableTileFieldVisibiltyTo.");
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
        return !UnitHasActedThisRound && ControllerContainer.BattleController.GetCurrentlyPlayingTeam().m_TeamColor == TeamAffinity.m_TeamColor;
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
    public void DisplayRouteToDestination(List<Vector2> routeToDestination)
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

        ControllerContainer.BattleController.AddOnConfirmButtonPressedListener(() =>
        {
            ControllerContainer.BattleController.RemoveCurrentConfirmButtonPressedListener();
            StartCoroutine(MoveAlongRoute(routeToDestination, null));
        });
    }

    /// <summary>
    /// Moves the along route.
    /// </summary>
    /// <param name="route">The route.</param>
    /// <param name="onMoveFinished">The on move finished.</param>
    /// <returns></returns>
    public IEnumerator MoveAlongRoute(List<Vector2> route, Action onMoveFinished)
    {
        // Starting with an index of 1 here, because the node at index 0 is the node the unit is standing on.
        for (int nodeIndex = 1; nodeIndex < route.Count; nodeIndex++)
        {
            Vector2 nodeToMoveTo = route[nodeIndex];
            Vector2 currentNode = route[nodeIndex - 1];

            yield return MoveToNeighborNode(currentNode, nodeToMoveTo);

            if (nodeIndex == route.Count - 1)
            {
                UnitHasActedThisRound = true;

                if (onMoveFinished != null)
                {
                    onMoveFinished();
                }
            }
        }
    }

    /// <summary>
    /// Moves from to neighbor node.
    /// </summary>
    /// <param name="startNode">The start node.</param>
    /// <param name="destinationNode">The destination node.</param>
    public IEnumerator MoveToNeighborNode(Vector2 startNode, Vector2 destinationNode)
    {
        Vector2 nodePositionDiff = startNode - destinationNode;

        // Rotate unit to destination node
        CardinalDirection directionToRotateTo = ControllerContainer.TileNavigationController.GetCardinalDirectionFromNodePositionDiff(
            nodePositionDiff, false);

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
                m_currentSimplifiedPosition = mapTile.SimplifiedMapPosition;
                yield break;
            }

            yield return null;
        }
    }
}
