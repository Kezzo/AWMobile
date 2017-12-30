using System;
using System.Collections.Generic;
using AWM.BattleMechanics;
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
            Dictionary<TeamColor, List<BaseUnit>> units = CC.BSC.RegisteredTeams;
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
                    if (CC.BSC.HasBattleEnded)
                    {
                        return;
                    }

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
                CC.BSC.EndCurrentTurn();
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
            IMovementCostResolver movementCostResolver = new UnitBalancingMovementCostResolver(unitToProcessTurnFor.GetUnitBalancing());

            BaseUnit unitToAttack;
            if (!TryToGetUnitToAttack(unitToProcessTurnFor, out unitToAttack))
            {
                IdleUnitAround(unitToProcessTurnFor, unitTurnProcessingDoneCallback, movementCostResolver);
                return;
            }

            List<BaseMapTile> mapTilesToAttackFrom;
            if (!TryToGetPositionsSortedByPreferenceToAttackGivenUnitFrom(unitToProcessTurnFor, unitToAttack, movementCostResolver, out mapTilesToAttackFrom))
            {
                IdleUnitAround(unitToProcessTurnFor, unitTurnProcessingDoneCallback, movementCostResolver);
                return;
            }

            if (TryToMoveToUnitAndAttack(unitToProcessTurnFor, unitTurnProcessingDoneCallback, unitToAttack, movementCostResolver, mapTilesToAttackFrom))
            {
                return;
            }

            if (!TryToMoveTowardsPosition(unitToProcessTurnFor, unitTurnProcessingDoneCallback, mapTilesToAttackFrom, movementCostResolver))
            {
                IdleUnitAround(unitToProcessTurnFor, unitTurnProcessingDoneCallback, movementCostResolver);
            }
        }

        /// <summary>
        /// Will move the unit around to let it idle and potentially unblock a path.
        /// </summary>
        /// <param name="unitToProcessTurnFor">The unit to process turn for.</param>
        /// <param name="unitTurnProcessingDoneCallback">The unit turn processing done callback.</param>
        /// <param name="movementCostResolver">The movement cost resolver.</param>
        private void IdleUnitAround(BaseUnit unitToProcessTurnFor, Action unitTurnProcessingDoneCallback, IMovementCostResolver movementCostResolver)
        {
            List<BaseMapTile> walkableMapTiles = CC.TNC.GetWalkableMapTiles(unitToProcessTurnFor.CurrentSimplifiedPosition, movementCostResolver);

            if (walkableMapTiles != null && walkableMapTiles.Count > 0)
            {
                walkableMapTiles.Sort((mapTile1, mapTile2) =>
                {
                    int comparisonValue = CC.TNC.GetDistanceToCoordinate(mapTile2.m_SimplifiedMapPosition, unitToProcessTurnFor.CurrentSimplifiedPosition)
                        .CompareTo(CC.TNC.GetDistanceToCoordinate(mapTile1.m_SimplifiedMapPosition, unitToProcessTurnFor.CurrentSimplifiedPosition));

                    if (comparisonValue == 0)
                    {
                        comparisonValue = Random.Range(-1, 2);
                    }

                    return comparisonValue;
                });

                if(!TryToMoveTowardsPosition(unitToProcessTurnFor, unitTurnProcessingDoneCallback, walkableMapTiles, movementCostResolver))
                {
                    unitTurnProcessingDoneCallback();
                }
            }
            else
            {
                unitTurnProcessingDoneCallback();
            }
        }

        /// <summary>
        /// Processes the unit movement when the target is in range.
        /// </summary>
        /// <param name="unitToProcessTurnFor">The unit to process turn for.</param>
        /// <param name="unitTurnProcessingDoneCallback">The unit turn processing done callback.</param>
        /// <param name="unitToAttack">The unit to attack.</param>
        /// <param name="movementCostResolver">The movement cost resolver.</param>
        /// <param name="tilesToAttackFrom">The tiles the unit can be attack from, sorted by how they're prefered (to keep distance & closest to attacking unit)</param>
        /// <returns>Returns true when the unit was able to move to the enemy and attack; otherwise false.</returns>
        private bool TryToMoveToUnitAndAttack(BaseUnit unitToProcessTurnFor, Action unitTurnProcessingDoneCallback, 
            BaseUnit unitToAttack, IMovementCostResolver movementCostResolver, List<BaseMapTile> tilesToAttackFrom)
        {
            // Attack immediately if prefered maptile to attack from (index 0) is current attacking unit position; 
            // otherwise a more prefered position should be taken.
            if (tilesToAttackFrom[0].m_SimplifiedMapPosition == unitToProcessTurnFor.CurrentSimplifiedPosition)
            {
                AttackUnitIfInRange(unitToProcessTurnFor, unitToAttack, unitTurnProcessingDoneCallback);
                return true;
            }

            List<BaseMapTile> tilesInRange = new List<BaseMapTile>(tilesToAttackFrom);

            // Filter unreachable tiles and potentially exit early.
            for (int i = tilesInRange.Count - 1; i >= 0; i--)
            {
                if (CC.TNC.GetDistanceToCoordinate(tilesInRange[i].m_SimplifiedMapPosition, unitToProcessTurnFor.CurrentSimplifiedPosition)
                    > unitToProcessTurnFor.GetUnitBalancing().m_MovementRangePerRound)
                {
                    tilesInRange.RemoveAt(i);
                }
            }

            if (tilesInRange.Count == 0)
            {
                return false;
            }

            // Iterate through reachable maptiles until a route was found.
            foreach (var tileToAttackFrom in tilesInRange)
            {
                List<Vector2> routeToMove = CC.TNC.GetBestWayToDestination(unitToProcessTurnFor.CurrentSimplifiedPosition, 
                    tileToAttackFrom.m_SimplifiedMapPosition, movementCostResolver);

                if (routeToMove != null)
                {
                    unitToProcessTurnFor.MoveAlongRoute(routeToMove, movementCostResolver, null,
                        () => AttackUnitIfInRange(unitToProcessTurnFor, unitToAttack, unitTurnProcessingDoneCallback));

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Processes the unit movement when the target is out of range.
        /// </summary>
        /// <param name="unitToProcessTurnFor">The unit to process turn for.</param>
        /// <param name="unitTurnProcessingDoneCallback">The unit turn processing done callback.</param>
        /// <param name="mapTilesToMoveTowards">The tiles the unit can be attack from, sorted by how they're prefered (to keep distance & closest to attacking unit)</param>
        /// <param name="movementCostResolver">The movement cost resolver.</param>
        private bool TryToMoveTowardsPosition(BaseUnit unitToProcessTurnFor, Action unitTurnProcessingDoneCallback, 
            List<BaseMapTile> mapTilesToMoveTowards, IMovementCostResolver movementCostResolver)
        {
            foreach (var mapTileToMoveToward in mapTilesToMoveTowards)
            {
                List<Vector2> longTermRouteToEnemy = CC.TNC.GetBestWayToDestination(unitToProcessTurnFor.CurrentSimplifiedPosition, 
                    mapTileToMoveToward.m_SimplifiedMapPosition, new EndlessRangeUnitBalancingMovementCostResolver(
                        unitToProcessTurnFor.GetUnitBalancing(), mapTileToMoveToward.m_SimplifiedMapPosition));

                if (longTermRouteToEnemy != null)
                {
                    List<Vector2> routeToMove = CC.TNC.ExtractWalkableRangeFromRoute(
                        unitToProcessTurnFor, movementCostResolver, longTermRouteToEnemy);

                    if (routeToMove.Count > 0)
                    {
                        unitToProcessTurnFor.MoveAlongRoute(routeToMove, movementCostResolver, null,
                            unitTurnProcessingDoneCallback);
                    }
                    else
                    {
                        unitTurnProcessingDoneCallback();
                    }

                    return true;
                }
            }

            return false;
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
            if (!CC.BSC.IsUnitInAttackRange(attackingUnit, unitToAttack))
            {
                onAttackDoneCallback();
                return;
            }

            attackingUnit.AttackUnit(unitToAttack, () =>
            {
                // Check if unit died. Can't check unit itself, since it will be removed immediately when dying.
                if (!CC.BSC.IsUnitOnNode(unitToAttack.CurrentSimplifiedPosition))
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
                if (!CC.BSC.IsUnitInAttackRange(attackingUnit, unit1) ||
                    !CC.BSC.IsUnitInAttackRange(attackingUnit, unit2))
                {
                    // Compare the range to the unit to attack
                    unitComparisonValue = CC.TNC.GetDistanceToCoordinate(
                        positionOfAttackingUnit, unit1.CurrentSimplifiedPosition)
                    .CompareTo(CC.TNC.GetDistanceToCoordinate(
                        positionOfAttackingUnit, unit2.CurrentSimplifiedPosition));
                }

                // The range to the unit to the attack is equal
                if (unitComparisonValue == 0)
                {
                    // Test against how much damage can be done
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
        /// <param name="movementCostResolver">The movement cost resolver to use.</param>
        /// <param name="mapTilesToAttackFrom">The walkable tiles closest to the next attackable enemy if available; otherwise null.</param>
        /// <returns>Returns true, when a maptile to move to was found; otherwise false.</returns>
        private bool TryToGetPositionsSortedByPreferenceToAttackGivenUnitFrom(BaseUnit unit, BaseUnit enemyToAttack, 
            IMovementCostResolver movementCostResolver, out List<BaseMapTile> mapTilesToAttackFrom)
        {
            mapTilesToAttackFrom = new List<BaseMapTile>();
            Vector2 closestEnemyPosition = enemyToAttack.CurrentSimplifiedPosition;

            List<BaseMapTile> mapTilesInAttackRange = CC.TNC.GetMapTilesInRange(closestEnemyPosition,
                unit.GetUnitBalancing().m_AttackRange);

            if (mapTilesInAttackRange.Count == 0)
            {
                return false;
            }

            // get list of maptiles where closest enemy is attackable from
            foreach (var walkableTile in mapTilesInAttackRange)
            {
                if (movementCostResolver.CanUnitWalkOnMapTile(walkableTile, 0, false) || 
                    walkableTile.m_SimplifiedMapPosition == unit.CurrentSimplifiedPosition)
                {
                    mapTilesToAttackFrom.Add(walkableTile);
                }
            }

            if (mapTilesToAttackFrom.Count == 0)
            {
                return false;
            }

            // find maptile with max range where the unit can still attack
            mapTilesToAttackFrom.Sort((mapTile1, mapTile2) =>
            {
                int mapTileDistanceComparison = CC.TNC.GetDistanceToCoordinate(
                    mapTile2.m_SimplifiedMapPosition, closestEnemyPosition)
                    .CompareTo(CC.TNC.GetDistanceToCoordinate(
                        mapTile1.m_SimplifiedMapPosition, closestEnemyPosition));

                // if the distance to the enemy position is equal, find the one closest to the attack unit to make it more realistic.
                if (mapTileDistanceComparison == 0)
                {
                    mapTileDistanceComparison = CC.TNC.GetDistanceToCoordinate(
                        mapTile1.m_SimplifiedMapPosition, unit.CurrentSimplifiedPosition)
                        .CompareTo(CC.TNC.GetDistanceToCoordinate(
                            mapTile2.m_SimplifiedMapPosition, unit.CurrentSimplifiedPosition));
                }

                return mapTileDistanceComparison;
            });

            return true;
        }
    }
}
