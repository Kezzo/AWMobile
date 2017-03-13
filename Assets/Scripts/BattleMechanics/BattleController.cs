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

    private Action m_onConfirmMoveButtonPressed;

    private readonly Dictionary<string, Action<Team[]>> m_onBattleStartEvents = new Dictionary<string, Action<Team[]>>();
    private readonly Dictionary<string, Action<Team>> m_onTurnStartEvents = new Dictionary<string, Action<Team>>();

    private Dictionary<TeamColor, List<BaseUnit>> m_registeredUnits;
    public Dictionary<TeamColor, List<BaseUnit>> RegisteredUnits { get { return m_registeredUnits; } }

    private List<int> m_uniqueUnitIdents;

    private List<AIController> m_registeredAIs;

    private float m_zoomLevelInPlayerTurn;

    /// <summary>
    /// Initializes a battle.
    /// </summary>
    /// <param name="teamsThisBattle">The teams this battle.</param>
    public void IntializeBattle(Team[] teamsThisBattle)
    {
        m_teamThisBattle = teamsThisBattle;
        m_subTurnCount = 0;
        m_turnCount = 0;

        m_registeredUnits = new Dictionary<TeamColor, List<BaseUnit>>();
        m_uniqueUnitIdents = new List<int>();

        m_onConfirmMoveButtonPressed = null;
        m_onTurnStartEvents.Clear();
        m_onBattleStartEvents.Clear();

        m_registeredAIs = new List<AIController>();

        foreach (Team team in m_teamThisBattle)
        {
            List<BaseUnit> teamUnitList = new List<BaseUnit>();
            m_registeredUnits.Add(team.m_TeamColor, teamUnitList);
            if (!team.m_IsPlayersTeam)
            {
                m_registeredAIs.Add(new AIController(team));
            }
        }
    }

    /// <summary>
    /// Starts the previously initialized battle.
    /// </summary>
    public void StartBattle()
    {
        foreach (var battleStartEvent in m_onBattleStartEvents)
        {
            battleStartEvent.Value(m_teamThisBattle);
        }

        StartNextTurn();
    }

    /// <summary>
    /// Registers a unit and returns a unique id for the unit, so we can always identify a certain unit.
    /// </summary>
    /// <param name="teamColor">Color of the team.</param>
    /// <param name="baseUnit">The base unit.</param>
    /// <returns></returns>
    public int RegisterUnit(TeamColor teamColor, BaseUnit baseUnit)
    {
        m_registeredUnits[teamColor].Add(baseUnit);

        int unitIdent = m_uniqueUnitIdents.Count > 0 ? m_uniqueUnitIdents[m_uniqueUnitIdents.Count - 1] + 1 : 0;

        m_uniqueUnitIdents.Add(unitIdent);

        return unitIdent;
    }

    /// <summary>
    /// Removes the registered unit.
    /// </summary>
    /// <param name="teamColor">Color of the team.</param>
    /// <param name="baseUnit">The base unit.</param>
    public void RemoveRegisteredUnit(TeamColor teamColor, BaseUnit baseUnit)
    {
        m_registeredUnits[teamColor].Remove(baseUnit);
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
        foreach (var item in m_registeredUnits)
        {
            if (item.Value.Exists(unit => unit.CurrentSimplifiedPosition == node))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the unit on node.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <returns></returns>
    public BaseUnit GetUnitOnNode(Vector2 node)
    {
        foreach (var item in m_registeredUnits)
        {
            BaseUnit foundUnit = item.Value.Find(unit => unit.CurrentSimplifiedPosition == node);
            if (foundUnit != null)
            {
                return foundUnit;
            }
        }

        return null;
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
    private void StartNextTurn()
    {
        Team teamToStartNext = m_teamThisBattle[m_subTurnCount];

        Debug.LogFormat("Starting turn for Team: '{0}'", teamToStartNext.m_TeamColor);

        foreach (var turnStartEvent in m_onTurnStartEvents)
        {
            turnStartEvent.Value(teamToStartNext);
        }

        if (!teamToStartNext.m_IsPlayersTeam)
        {
            foreach (AIController ai in m_registeredAIs)
            {
                if (teamToStartNext.m_TeamColor == ai.MyTeamColor)
                {
                    ai.StartTurn();
                }
            }
        }
    }

    /// <summary>
    /// Ends the current turn.
    /// </summary>
    public void EndCurrentTurn()
    {
        ZoomCameraBasedOnTheCurrentTeam();

        List<BaseUnit> unitsToReset = m_registeredUnits[GetCurrentlyPlayingTeam().m_TeamColor];

        for (int unitIndex = 0; unitIndex < unitsToReset.Count; unitIndex++)
        {
            unitsToReset[unitIndex].ResetUnit();
        }

        m_subTurnCount++;

        if (m_subTurnCount == m_teamThisBattle.Length)
        {
            m_subTurnCount = 0;
            m_turnCount++;
        }

        StartNextTurn();
    }

    /// <summary>
    /// Zooms the camera based on the currently playing team.
    /// </summary>
    private void ZoomCameraBasedOnTheCurrentTeam()
    {
        CameraControls cameraController;
        if (ControllerContainer.MonoBehaviourRegistry.TryGet(out cameraController))
        {
            float zoomLevel;

            if (GetCurrentlyPlayingTeam().m_IsPlayersTeam)
            {
                m_zoomLevelInPlayerTurn = cameraController.CurrentZoomLevel;
                zoomLevel = 10f;
            }
            else
            {
                //TODO: Handle multiple enemy teams
                zoomLevel = m_zoomLevelInPlayerTurn;
            }

            cameraController.AutoZoom(zoomLevel);
        }
    }

    /// <summary>
    /// Adds a turn start event.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="actionToAdd">The action that should be called when a turn starts.</param>
    public void AddBattleStartedEvent(string key, Action<Team[]> actionToAdd)
    {
        if (m_onBattleStartEvents.ContainsKey(key))
        {
            Debug.LogError("Trying to add turn end event with already existing key!");
        }
        else
        {
            m_onBattleStartEvents.Add(key, actionToAdd);
        }
    }

    /// <summary>
    /// Removes a turn start event.
    /// </summary>
    /// <param name="key">The key.</param>
    public void RemoveBattleStartedEvent(string key)
    {
        m_onBattleStartEvents.Remove(key);
    }

    /// <summary>
    /// Adds a turn start event.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="actionToAdd">The action that should be called when a turn starts.</param>
    public void AddTurnStartEvent(string key, Action<Team> actionToAdd)
    {
        if (m_onTurnStartEvents.ContainsKey(key))
        {
            Debug.LogError("Trying to add turn end event with already existing key!");
        }
        else
        {
            m_onTurnStartEvents.Add(key, actionToAdd);
        }
    }

    /// <summary>
    /// Removes a turn start event.
    /// </summary>
    /// <param name="key">The key.</param>
    public void RemoveTurnStartEvent(string key)
    {
        m_onTurnStartEvents.Remove(key);
    }

    /// <summary>
    /// Adds the on confirm button pressed listener.
    /// </summary>
    /// <param name="actionToCall">The action to call.</param>
    public void AddConfirmMoveButtonPressedListener(Action actionToCall)
    {
        m_onConfirmMoveButtonPressed = actionToCall;
    }

    /// <summary>
    /// Removes the current confirm button pressed listener.
    /// </summary>
    public void RemoveCurrentConfirmMoveButtonPressedListener()
    {
        m_onConfirmMoveButtonPressed = null;
    }

    /// <summary>
    /// Called when the current move was confirmed.
    /// </summary>
    public void OnConfirmMove()
    {
        if (m_onConfirmMoveButtonPressed != null)
        {
            m_onConfirmMoveButtonPressed();
        }
    }

    /// <summary>
    /// Gets the units in range.
    /// </summary>
    /// <param name="sourceNode">The source node.</param>
    /// <param name="range">The range.</param>
    /// <returns></returns>
    public List<BaseUnit> GetUnitsInRange(Vector2 sourceNode, int range)
    {
        List<BaseUnit> unitsInRange = new List<BaseUnit>();

        TileNavigationController tileNavigationController = ControllerContainer.TileNavigationController;
        HashSet<Vector2> checkedNodes = new HashSet<Vector2> { sourceNode };

        Queue<Vector2> nodesToCheck = new Queue<Vector2>();
        nodesToCheck.Enqueue(sourceNode);

        while (true)
        {
            Vector2 nodeToCheck = nodesToCheck.Dequeue();

            // Get unit on Node
            if (nodeToCheck != sourceNode)
            {
                var unitOnNode = GetUnitOnNode(nodeToCheck);

                if (unitOnNode != null)
                {
                    unitsInRange.Add(unitOnNode);
                }
            }

            List<Vector2> adjacentNodes = tileNavigationController.GetAdjacentNodes(nodeToCheck)
                .FindAll(node => !checkedNodes.Contains(node));

            for (int nodeIndex = 0; nodeIndex < adjacentNodes.Count; nodeIndex++)
            {
                Vector2 adjacentNode = adjacentNodes[nodeIndex];

                checkedNodes.Add(adjacentNode);

                // Is node in range?
                if (tileNavigationController.GetDistanceToCoordinate(sourceNode, adjacentNode) <= range)
                {
                    nodesToCheck.Enqueue(adjacentNode);
                }
            }

            if (nodesToCheck.Count == 0)
            {
                break;
            }
        }

        return unitsInRange;
    }
}
