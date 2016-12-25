using System.Collections.Generic;
using UnityEngine;

public class BaseUnit : MonoBehaviour
{
    [SerializeField]
    private GameObject m_selectionMarker;

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
            ControllerContainer.TileNavigationController.RegisterUnit(this);
        }
        
        // Load balancing once here and keep for the round.
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
        m_currentWalkableMapTiles = null;
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
}
