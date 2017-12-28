using System;
using System.Collections.Generic;
using AWM.BattleMechanics;
using AWM.EditorAndDebugOnly;
using AWM.Enums;
using AWM.MapTileGeneration;
using AWM.Models;
using AWM.System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AWM.AI
{
    /// <summary>
    /// AI that controls one Player in the game.
    /// </summary>
    public class AIController
    {
        /// <summary>
        /// The team the AI is playing for.
        /// </summary>
        private readonly Team m_myTeam;

        /// <summary>
        /// Every Unit under this AI's Control.
        /// </summary>
        private List<BaseUnit> m_aiUnits;

        /// <summary>
        /// Every Unit not under the AI's Control.
        /// </summary>
        private List<BaseUnit> m_enemyUnits;

        /// <summary>
        /// The position at which Unit the AI is while moving them.
        /// </summary>
        private int m_unitCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="AIController"/> class.
        /// </summary>
        /// <param name="aiTeam">The AI team.</param>
        public AIController(Team aiTeam)
        {
            m_myTeam = aiTeam;
            m_enemyUnits = new List<BaseUnit>();
        }

        /// <summary>
        /// Gets the color of the AI's team.
        /// </summary>
        /// <value>
        /// The color of my team.
        /// </value>
        public TeamColor MyTeamColor
        {
            get { return this.m_myTeam.m_TeamColor; }
        }

        /// <summary>
        /// Starts the turn.
        /// </summary>
        public void StartTurn()
        {
            Dictionary<TeamColor, List<BaseUnit>> units = ControllerContainer.BattleStateController.RegisteredTeams;
            m_aiUnits = units[this.MyTeamColor];
            m_enemyUnits = new List<BaseUnit>();

            foreach (var item in units)
            {
                if (item.Key != m_myTeam.m_TeamColor)
                {
                    m_enemyUnits.AddRange(item.Value);
                }
            }

            // to avoid same move sequence
            ListHelper.ShuffleList(ref m_aiUnits);

            m_unitCounter = 0;
            MoveNextUnit();
        }

        /// <summary>
        /// Moves the next unit.
        /// </summary>
        private void MoveNextUnit()
        {
            if (m_unitCounter < m_aiUnits.Count)
            {
                // To pause between each unit that was moved
                Root.Instance.CoroutineHelper.CallDelayed(m_aiUnits[m_unitCounter], 0.2f, () =>
                {
                    ProcessTurnOfUnit(m_aiUnits[m_unitCounter], () =>
                    {
                        m_aiUnits[m_unitCounter].UnitHasAttackedThisRound = true;
                        m_unitCounter++;
                        MoveNextUnit();
                    });
                });
            }
            else
            {
                m_unitCounter = 0;
                ControllerContainer.BattleStateController.EndCurrentTurn();
            }
        }

        /// <summary>
        /// Will process a turn for a given unit. That includes finding the best enemy unit to attack, 
        /// moving to it or as close as possbile and attacking it.
        /// </summary>
        /// <param name="unitToProcessTurnFor">The unit for which a turn should be processed.</param>
        /// <param name="unitTurnProcessingDoneCallback">Will be invoked when the turn process is done.</param>
        private void ProcessTurnOfUnit(BaseUnit unitToProcessTurnFor, Action unitTurnProcessingDoneCallback)
        {
            BaseUnit unitToAttack;
            if (!TryToGetUnitToAttack(unitToProcessTurnFor, out unitToAttack))
            {
                unitTurnProcessingDoneCallback();
                return;
            }

            BaseMapTile tileToWalkTo;
            if (TryBestPositionToAttackGivenUnitFrom(unitToProcessTurnFor, unitToAttack, out tileToWalkTo))
            {
                if (tileToWalkTo.m_SimplifiedMapPosition == unitToProcessTurnFor.CurrentSimplifiedPosition)
                {
                    AttackUnitIfInRange(unitToProcessTurnFor, unitToAttack, unitTurnProcessingDoneCallback);
                }
                else
                {
                    IMovementCostResolver movementCostResolver = new UnitBalancingMovementCostResolver(
                        unitToProcessTurnFor.GetUnitBalancing());

                    List<Vector2> routeToMove = ControllerContainer.TileNavigationController.GetBestWayToDestination(
                        unitToProcessTurnFor.CurrentSimplifiedPosition, tileToWalkTo.m_SimplifiedMapPosition,
                        movementCostResolver);

                    unitToProcessTurnFor.MoveAlongRoute(routeToMove, movementCostResolver,
                        null, () => AttackUnitIfInRange(unitToProcessTurnFor, unitToAttack, unitTurnProcessingDoneCallback));
                }
            }
            else
            {
                unitTurnProcessingDoneCallback();
            }
        }

        /// <summary>
        /// Tries to find the closest unit a given unit can attack.
        /// </summary>
        /// <param name="attackingUnit">The unit to find a unit to attack for.</param>
        /// <param name="unitToAttack">The closest unit that can be attacked.</param>
        /// <returns>True when a unit to attack was found; otherwise false.</returns>
        private bool TryToGetUnitToAttack(BaseUnit attackingUnit, out BaseUnit unitToAttack)
        {
            List<BaseUnit> enemiesToCheck = new List<BaseUnit>(m_enemyUnits);
            enemiesToCheck.RemoveAll(enemyUnit => !attackingUnit.CanAttackUnit(enemyUnit));

            if (enemiesToCheck.Count == 0)
            {
                unitToAttack = null;
                return false;
            }

            SortListByEffectivityAgainstUnit(attackingUnit, ref enemiesToCheck);

            unitToAttack = enemiesToCheck[0];
            return true;
        }

        /// <summary>
        /// Will process the attack of a given unit on another given unit unit.
        /// </summary>
        private void AttackUnitIfInRange(BaseUnit attackingUnit, BaseUnit unitToAttack, Action onAttackDoneCallback)
        {
            if (!ControllerContainer.BattleStateController.IsUnitInAttackRange(attackingUnit, unitToAttack))
            {
                onAttackDoneCallback();
                return;
            }

            attackingUnit.AttackUnit(unitToAttack, () =>
            {
                // Check if unit died. Can't check unit itself, since it will be removed immediately when dying.
                if (!ControllerContainer.BattleStateController.IsUnitOnNode(unitToAttack.CurrentSimplifiedPosition))
                {
                    m_enemyUnits.Remove(unitToAttack);
                }

                onAttackDoneCallback();
            });
        }

        /// <summary>
        /// Sorts the list by how effective the attacking unit is against the unit in the list.
        /// Will sort units first by how close they're to the attacking unit. If there 
        /// </summary>
        /// <param name="attackingUnit"></param>
        /// <param name="listToSort">The list to sort.</param>
        private void SortListByEffectivityAgainstUnit(BaseUnit attackingUnit, ref List<BaseUnit> listToSort)
        {
            Vector2 positionOfAttackingUnit = attackingUnit.CurrentSimplifiedPosition;

            listToSort.Sort((unit1, unit2) =>
            {
                int unitComparisonValue = 0;

                // ignore distance comparison for all units in range.
                if (!ControllerContainer.BattleStateController.IsUnitInAttackRange(attackingUnit, unit1) ||
                    !ControllerContainer.BattleStateController.IsUnitInAttackRange(attackingUnit, unit2))
                {
                    unitComparisonValue = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                        positionOfAttackingUnit, unit1.CurrentSimplifiedPosition)
                    .CompareTo(ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                        positionOfAttackingUnit, unit2.CurrentSimplifiedPosition));
                }

                // The attack unit does equal damage the compared units.
                if (unitComparisonValue == 0)
                {
                    // Compare the range to the unit to attack
                    unitComparisonValue = attackingUnit.GetDamageOnUnit(unit2).CompareTo(attackingUnit.GetDamageOnUnit(unit1));
                }

                return unitComparisonValue;
            });
        }

        /// <summary>
        /// Will try to find the best route or closest maptile to the given <see cref="BaseUnit"/> to attack.
        /// </summary>
        /// <param name="unit">The unit the maptile should be found for.</param>
        /// <param name="enemyToAttack">The unit that should be attacked.</param>
        /// <param name="maptTileToMoveTo">The walkable tile closest to the next attackable enemy if available; otherwise null.</param>
        /// <returns>Returns true, when a maptile to move to was found; otherwise false.</returns>
        private bool TryBestPositionToAttackGivenUnitFrom(BaseUnit unit, BaseUnit enemyToAttack, out BaseMapTile maptTileToMoveTo)
        {
            List<BaseMapTile> walkableTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(
                unit.CurrentSimplifiedPosition, new UnitBalancingMovementCostResolver(unit.GetUnitBalancing()));
            
            // to also test the current unit position.
            walkableTiles.Add(ControllerContainer.TileNavigationController.GetMapTile(unit.CurrentSimplifiedPosition));

            if (walkableTiles.Count == 0)
            {
                maptTileToMoveTo = null;
                return false;
            }
            
            Vector2 closestEnemyPosition = enemyToAttack.CurrentSimplifiedPosition;

            List<BaseMapTile> mapTilesToAttackFrom = new List<BaseMapTile>();

            // get list of maptiles where closest enemy is attackable from
            foreach (var walkableTile in walkableTiles)
            {
                if (ControllerContainer.TileNavigationController.GetDistanceToCoordinate(walkableTile.m_SimplifiedMapPosition, 
                    closestEnemyPosition) <= unit.GetUnitBalancing().m_AttackRange)
                {
                    mapTilesToAttackFrom.Add(walkableTile);
                }
            }

            if (mapTilesToAttackFrom.Count > 0)
            {
                // find maptile with max range where the unit can still attack
                mapTilesToAttackFrom.Sort((mapTile1, mapTile2) =>
                {
                    int mapTileDistanceComparison = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                        mapTile2.m_SimplifiedMapPosition, closestEnemyPosition)
                        .CompareTo(ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                            mapTile1.m_SimplifiedMapPosition, closestEnemyPosition));

                    // if the distance to the enemy position is equal, find the one closest to the attack unit to make it more realistic.
                    if (mapTileDistanceComparison == 0)
                    {
                        mapTileDistanceComparison = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                            mapTile1.m_SimplifiedMapPosition, unit.CurrentSimplifiedPosition)
                            .CompareTo(ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                                mapTile2.m_SimplifiedMapPosition, unit.CurrentSimplifiedPosition));
                    }

                    return mapTileDistanceComparison;
                });

                maptTileToMoveTo = mapTilesToAttackFrom[0];
            }
            else
            {
                // Find closest walkable (also no unit on it) maptile to attackable unit
                walkableTiles.Sort((mapTile1, mapTile2) =>
                {
                    int mapTileDistanceComparison = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                        mapTile1.m_SimplifiedMapPosition, closestEnemyPosition)
                        .CompareTo(ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                            mapTile2.m_SimplifiedMapPosition, closestEnemyPosition));

                    if (mapTileDistanceComparison == 0)
                    {
                        mapTileDistanceComparison = Random.Range(-1, 2);
                    }

                    return mapTileDistanceComparison;
                });

                maptTileToMoveTo = walkableTiles[0];
            }
            
            return true;
        }
    }
}
