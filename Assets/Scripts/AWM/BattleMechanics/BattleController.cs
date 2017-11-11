using System;
using System.Collections.Generic;
using AWM.AI;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEngine;

namespace AWM.BattleMechanics
{
    public class BattleController
    {
        private Team[] m_teamThisBattle;
        private string m_levelNameOfBattle;

        // Defines the turn of one round. After the round ends and another battle turn begins, this gets reset to 0.
        private int m_subTurnCount;

        // Defines the turn of the battle.
        private int m_turnCount;

        private Action m_onConfirmMoveButtonPressed;

        private readonly Dictionary<string, Action<Team[]>> m_onBattleStartEvents = new Dictionary<string, Action<Team[]>>();
        private readonly Dictionary<string, Action<Team>> m_onTurnStartEvents = new Dictionary<string, Action<Team>>();
        private readonly Dictionary<string, Action<TeamColor>> m_onTeamWonEvents = new Dictionary<string, Action<TeamColor>>();

        private Dictionary<TeamColor, List<BaseUnit>> m_registeredTeams;
        public Dictionary<TeamColor, List<BaseUnit>> RegisteredTeams { get { return m_registeredTeams; } }

        private List<int> m_uniqueUnitIdents;

        private List<AIController> m_registeredAIs;

        /// <summary>
        /// Initializes a battle.
        /// </summary>
        /// <param name="teamsThisBattle">The teams this battle.</param>
        /// <param name="levelName">The name of the level to initialize.</param>
        public void IntializeBattle(Team[] teamsThisBattle, string levelName)
        {
            m_teamThisBattle = teamsThisBattle;
            m_levelNameOfBattle = levelName;
            m_subTurnCount = 0;
            m_turnCount = 0;

            m_registeredTeams = new Dictionary<TeamColor, List<BaseUnit>>();
            m_uniqueUnitIdents = new List<int>();

            m_registeredAIs = new List<AIController>();

            foreach (Team team in m_teamThisBattle)
            {
                List<BaseUnit> teamUnitList = new List<BaseUnit>();
                m_registeredTeams.Add(team.m_TeamColor, teamUnitList);
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
        /// Called when a battle happened.
        /// Will check if now all units in a team are dead to end the match.
        /// </summary>
        public void OnBattleDone()
        {
            List<TeamColor> stillPlayingTeams = new List<TeamColor>();

            foreach (var team in m_registeredTeams)
            {
                if (team.Value.Exists(unit => !unit.StatManagement.IsDead))
                {
                    stillPlayingTeams.Add(team.Key);
                }
            }

            // all units of a team is dead
            if (stillPlayingTeams.Count == 1)
            {
                if (IsTeamWithColorPlayersTeam(stillPlayingTeams[0]))
                {
                    ControllerContainer.PlayerProgressionService.TrackLevelAsCompleted(m_levelNameOfBattle);
                }

                foreach (var onTeamWonEvent in m_onTeamWonEvents)
                {
                    onTeamWonEvent.Value(stillPlayingTeams[0]);
                }
            }
        }

        /// <summary>
        /// Registers a unit and returns a unique id for the unit, so we can always identify a certain unit.
        /// </summary>
        /// <param name="teamColor">Color of the team.</param>
        /// <param name="baseUnit">The base unit.</param>
        /// <returns></returns>
        public int RegisterUnit(TeamColor teamColor, BaseUnit baseUnit)
        {
            m_registeredTeams[teamColor].Add(baseUnit);

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
            m_registeredTeams[teamColor].Remove(baseUnit);
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
            foreach (var item in m_registeredTeams)
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
            foreach (var item in m_registeredTeams)
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
        /// Determines if the given teamcolor is the color of the players team.
        /// </summary>
        /// <param name="playerTeamColor">Color of the player team.</param>
        private bool IsTeamWithColorPlayersTeam(TeamColor playerTeamColor)
        {
            foreach (var team in m_teamThisBattle)
            {
                if (team.m_TeamColor == playerTeamColor)
                {
                    return team.m_IsPlayersTeam;
                }
            }

            return false;
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
            List<BaseUnit> unitsToReset = m_registeredTeams[GetCurrentlyPlayingTeam().m_TeamColor];

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
        /// Adds a turn end event.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="actionToAdd">The action that should be called when a turn ends.</param>
        public void AddBattleEndedEvent(string key, Action<TeamColor> actionToAdd)
        {
            if (m_onTeamWonEvents.ContainsKey(key))
            {
                Debug.LogError("Trying to add turn end event with already existing key!");
            }
            else
            {
                m_onTeamWonEvents.Add(key, actionToAdd);
            }
        }

        /// <summary>
        /// Removes a turn end event.
        /// </summary>
        /// <param name="key">The key.</param>
        public void RemoveBattleEndedEvent(string key)
        {
            m_onTeamWonEvents.Remove(key);
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
}
