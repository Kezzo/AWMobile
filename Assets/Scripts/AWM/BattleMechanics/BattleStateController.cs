using System;
using System.Collections.Generic;
using AWM.AI;
using AWM.Audio;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEngine;

namespace AWM.BattleMechanics
{
    /// <summary>
    /// Holds, controls and published data about the state of a running battle. 
    /// A new instance of this class is created for each match.
    /// </summary>
    public class BattleStateController
    {
        private Team[] m_teamThisBattle;
        private string m_levelNameOfBattle;

        // Defines the turn of one round. After the round ends and another battle turn begins, this gets reset to 0.
        private int m_subTurnCount;

        // Defines the turn of the battle.
        private int m_turnCount;

        private Action m_onConfirmMoveButtonPressed;

        public Dictionary<string, Action<Team[]>> OnBattleStartListener { get; private set; }
        public Dictionary<string, Action<Team>> OnTurnStartListener { get; private set; }
        public Dictionary<string, Action<TeamColor>> OnTeamWonListener { get; private set; }
        public Dictionary<int, Action<bool>> OnUnitSelectedListener { get; private set; }
        public Dictionary<string, Action<TeamColor>> OnTeamDoneThisTurnListener { get; private set; }

        private Dictionary<TeamColor, List<BaseUnit>> m_registeredTeams;
        public Dictionary<TeamColor, List<BaseUnit>> RegisteredTeams { get { return m_registeredTeams; } }

        private List<int> m_uniqueUnitIdents;

        private List<AIController> m_registeredAIs;

        public bool IsBattlePaused { get; set; }
        public bool HasBattleEnded { get; private set; }

        public BattleStateController()
        {
            m_registeredTeams = new Dictionary<TeamColor, List<BaseUnit>>();
            m_uniqueUnitIdents = new List<int>();

            m_registeredAIs = new List<AIController>();

            OnBattleStartListener = new Dictionary<string, Action<Team[]>>();
            OnTurnStartListener = new Dictionary<string, Action<Team>>();
            OnTeamWonListener = new Dictionary<string, Action<TeamColor>>();
            OnUnitSelectedListener = new Dictionary<int, Action<bool>>();
            OnTeamDoneThisTurnListener = new Dictionary<string, Action<TeamColor>>();
        }

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
            foreach (var battleStartEvent in OnBattleStartListener)
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
                HasBattleEnded = true;

                if (IsTeamWithColorPlayersTeam(stillPlayingTeams[0]))
                {
                    CC.PPS.TrackLevelAsCompleted(m_levelNameOfBattle);
                }

                foreach (var onTeamWonEvent in OnTeamWonListener)
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
        /// Gets the enemy units of a given team color.
        /// </summary>
        /// <param name="teamColorToFindEnemyUnitsFor">The team color to find enemy units for.</param>
        public List<BaseUnit> GetEnemyUnits(TeamColor teamColorToFindEnemyUnitsFor)
        {
            List<BaseUnit> enemyUnits = new List<BaseUnit>();

            foreach (var registeredTeam in m_registeredTeams)
            {
                if (registeredTeam.Key != teamColorToFindEnemyUnitsFor)
                {
                    enemyUnits.AddRange(registeredTeam.Value);
                }
            }

            return enemyUnits;
        }

        /// <summary>
        /// Determines if the given teamcolor is the color of the players team.
        /// </summary>
        /// <param name="playerTeamColor">Color of the player team.</param>
        public bool IsTeamWithColorPlayersTeam(TeamColor playerTeamColor)
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

            //Debug.LogFormat("Starting turn for Team: '{0}'", teamToStartNext.m_TeamColor);

            foreach (var turnStartEvent in OnTurnStartListener)
            {
                turnStartEvent.Value(teamToStartNext);
            }

            if (!teamToStartNext.m_IsPlayersTeam)
            {
                foreach (AIController ai in m_registeredAIs)
                {
                    if (teamToStartNext.m_TeamColor == ai.TeamColorOfControlledUnits)
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
        /// Called when a unit changed it's selection.
        /// </summary>
        /// <param name="wasSelected">if set to <c>true</c> the unit was selected.</param>
        public void OnUnitChangedSelection(bool wasSelected)
        {
            foreach (var action in OnUnitSelectedListener.Values)
            {
                action(wasSelected);
            }
        }

        /// <summary>
        /// Should be invoked by each that finished it's turn.
        /// </summary>
        /// <param name="unitIdent">The ident of the unit</param>
        /// <param name="teamColorOfUnit">The color of the team the units belongs to.</param>
        public void OnUnitIsDoneThisTurn(int unitIdent, TeamColor teamColorOfUnit)
        {
            foreach (var baseUnit in m_registeredTeams[teamColorOfUnit])
            {
                if (!baseUnit.UnitHasAttackedThisRound)
                {
                    return;
                }
            }

            foreach (Action<TeamColor> action in OnTeamDoneThisTurnListener.Values)
            {
                action(teamColorOfUnit);
            }
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
            Root.Instance.AudioManager.PlaySFX(SoundEffect.MovementClick);
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

            TileNavigationController tileNavigationController = CC.TNC;
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

        /// <summary>
        /// Determines whether a unit can attack a given unit.
        /// </summary>
        /// <param name="attackingUnit">The attacking unit.</param>
        /// <param name="unitToAttack">The unit to attack.</param>
        public bool IsUnitInAttackRange(BaseUnit attackingUnit, BaseUnit unitToAttack)
        {
            return CC.TNC.GetDistanceToCoordinate(
                attackingUnit.CurrentSimplifiedPosition, unitToAttack.CurrentSimplifiedPosition) <=
                   attackingUnit.GetUnitBalancing().m_AttackRange;
        }
    }
}
