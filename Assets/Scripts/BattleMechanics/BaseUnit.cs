using UnityEngine;

public class BaseUnit : MonoBehaviour
{
    [SerializeField]
    private GameObject m_selectionMarker;

    public Team TeamAffinity { get; private set; }
    public UnitType UnitType { get; private set; }
    public bool UnitHasActedThisRound { get; private set; }

    /// <summary>
    /// Initializes the specified team.
    /// </summary>
    /// <param name="unitData">The unit data.</param>
    public void Initialize(MapGenerationData.Unit unitData)
    {
        TeamAffinity = unitData.m_Team;
        UnitType = unitData.m_UnitType;
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
    }

    /// <summary>
    /// Called when the unit was deselected.
    /// </summary>
    public void OnUnitWasDeselected()
    {
        m_selectionMarker.SetActive(false);
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
