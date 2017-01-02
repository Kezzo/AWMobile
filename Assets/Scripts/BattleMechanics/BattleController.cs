using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleController
{
    private Team[] m_teamThisBattle;

    // Defines the turn of one round. After the round ends and another battle turn begins, this gets reset to 0.
    private int m_subTurnCount;

    // Defines the turn of the battle.
    private int m_turnCount;

    private Action m_onConfirmButtonPressed;

    private List<BaseUnit> m_registeredUnits;

    /// <summary>
    /// Intializes a battle.
    /// </summary>
    /// <param name="teamsThisBattle">The teams this battle.</param>
    public void IntializeBattle(Team[] teamsThisBattle)
    {
        m_teamThisBattle = teamsThisBattle;
        m_subTurnCount = 0;
        m_turnCount = 0;

        m_registeredUnits = new List<BaseUnit>();
    }

    /// <summary>
    /// Registers the unit.
    /// </summary>
    /// <param name="baseUnit">The base unit.</param>
    public void RegisterUnit(BaseUnit baseUnit)
    {
        m_registeredUnits.Add(baseUnit);
    }

    /// <summary>
    /// Determines whether a unit is on the given node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns>
    ///   <c>true</c> if [is unit on node] [the specified node]; otherwise, <c>false</c>.
    /// </returns>
    public bool IsUnitOnNode(Vector2 node)
    {
        return m_registeredUnits.Exists(unit => unit.CurrentSimplifiedPosition == node);
    }

    /// <summary>
    /// Gets the currently playing team.
    /// </summary>
    /// <returns></returns>
    public Team GetCurrentlyPlayingTeam()
    {
        return m_teamThisBattle[m_subTurnCount];
    }

    /// <summary>
    /// Determines whether [is players turn].
    /// </summary>
    /// <returns>
    ///   <c>true</c> if [is players turn]; otherwise, <c>false</c>.
    /// </returns>
    public bool IsPlayersTurn()
    {
        return GetCurrentlyPlayingTeam().m_IsPlayersTeam;
    }

    /// <summary>
    /// Starts the next turn.
    /// </summary>
    public void StartNextTurn()
    {
        Team teamToStartNext = m_teamThisBattle[m_subTurnCount];

        List<BaseUnit> unitsToReset = m_registeredUnits.FindAll(unit => unit.TeamAffinity.m_TeamColor == teamToStartNext.m_TeamColor);

        for (int unitIndex = 0; unitIndex < unitsToReset.Count; unitIndex++)
        {
            unitsToReset[unitIndex].ResetUnit();
        }
    }

    /// <summary>
    /// Ends the current turn.
    /// </summary>
    public void EndCurrentTurn()
    {
        m_subTurnCount++;

        if (m_subTurnCount == m_teamThisBattle.Length)
        {
            m_subTurnCount = 0;
            m_turnCount++;
        }

        StartNextTurn();
    }

    /// <summary>
    /// Adds the on confirm button pressed listener.
    /// </summary>
    /// <param name="actionToCall">The action to call.</param>
    public void AddOnConfirmButtonPressedListener(Action actionToCall)
    {
        m_onConfirmButtonPressed = actionToCall;
    }

    /// <summary>
    /// Removes the current confirm button pressed listener.
    /// </summary>
    public void RemoveCurrentConfirmButtonPressedListener()
    {
        m_onConfirmButtonPressed = null;
    }

    /// <summary>
    /// Called when the current move was confirmed.
    /// </summary>
    public void OnConfirmMove()
    {
        if (m_onConfirmButtonPressed != null)
        {
            m_onConfirmButtonPressed();
        }
    }
}
