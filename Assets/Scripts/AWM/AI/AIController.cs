using System.Collections.Generic;
using AWM.BattleMechanics;
using AWM.EditorAndDebugOnly;
using AWM.Enums;
using AWM.MapTileGeneration;
using AWM.Models;
using AWM.System;
using UnityEngine;

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

        private BaseUnit CurrentlyControlledUnit
        {
            get { return this.m_aiUnits[this.m_unitCounter]; }
        }

        /// <summary>
        /// Starts the turn.
        /// </summary>
        public void StartTurn()
        {
            Dictionary<TeamColor, List<BaseUnit>> units = ControllerContainer.BattleController.RegisteredTeams;
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
                Root.Instance.CoroutineHelper.CallDelayed(CurrentlyControlledUnit, 0.2f, () =>
                {
                    BaseMapTile tileToWalkTo = null;

                    if (TryGetWalkableTileClosestToNextAttackableEnemy(CurrentlyControlledUnit, out tileToWalkTo))
                    {
                        var dontCare = new Dictionary<Vector2, PathfindingNodeDebugData>();

                        IMovementCostResolver movementCostResolver = new UnitBalancingMovementCostResolver(
                            CurrentlyControlledUnit.GetUnitBalancing());

                        List<Vector2> routeToMove = ControllerContainer.TileNavigationController.GetBestWayToDestination(
                            CurrentlyControlledUnit.CurrentSimplifiedPosition, tileToWalkTo.m_SimplifiedMapPosition,
                            movementCostResolver, out dontCare);

                        CurrentlyControlledUnit.MoveAlongRoute(routeToMove, movementCostResolver, null, AttackIfPossible);
                    }
                    else
                    {
                        AttackIfPossible();
                    }
                });
            }
            else
            {
                m_unitCounter = 0;
                ControllerContainer.BattleController.EndCurrentTurn();
            }
        }

        /// <summary>
        /// Attacks if possible.
        /// </summary>
        private void AttackIfPossible()
        {
            BaseUnit attackingUnit = CurrentlyControlledUnit;
            List<BaseUnit> unitsInRange = ControllerContainer.BattleController.GetUnitsInRange(
                attackingUnit.CurrentSimplifiedPosition, attackingUnit.GetUnitBalancing().m_AttackRange);

            SortListByEffectivityAgainstUnit(attackingUnit, ref unitsInRange);

            for (int i = 0; i < unitsInRange.Count; i++)
            {
                if (attackingUnit.CanAttackUnit(unitsInRange[i]))
                {
                    BaseUnit unitToAttack = unitsInRange[i];
                    Debug.LogFormat("{0} {1} attacks {2} {3}", attackingUnit.TeamColor, attackingUnit.UnitType, unitToAttack.TeamColor, unitToAttack.UnitType);

                    attackingUnit.AttackUnit(unitToAttack, () =>
                    {
                        // Check if unit died
                        if (!ControllerContainer.BattleController.IsUnitOnNode(unitToAttack.CurrentSimplifiedPosition))
                        {
                            this.m_enemyUnits.Remove(unitToAttack);
                        }

                        m_unitCounter++;
                        MoveNextUnit();
                    });

                    return;
                }
            }

            m_unitCounter++;
            MoveNextUnit();
        }

        /// <summary>
        /// Sorts the list by how effective the attacking unit is against the unit in the list.
        /// The sorting goes from most effective [0] -> least effective [Count]
        /// If the attacking unit does equal damage to two compared units, they will be sorted based on the range to the attacking unit.
        /// So the closer unit is sorted higher than the unit more far away.
        /// </summary>
        /// <param name="attackingUnit"></param>
        /// <param name="listToSort">The list to sort.</param>
        private void SortListByEffectivityAgainstUnit(BaseUnit attackingUnit, ref List<BaseUnit> listToSort)
        {
            TileNavigationController tileNavigationController = ControllerContainer.TileNavigationController;
            Vector2 positionOfAttackingUnit = attackingUnit.CurrentSimplifiedPosition;

            listToSort.Sort((unit1, unit2) =>
            {
                int unitComparisonValue = attackingUnit.GetDamageOnUnit(unit2).CompareTo(attackingUnit.GetDamageOnUnit(unit1));

                // The attack unit does equal damage the compared units.
                if (unitComparisonValue == 0)
                {
                    // Compare the range to the unit to attack
                    unitComparisonValue = tileNavigationController.GetDistanceToCoordinate(positionOfAttackingUnit, unit1.CurrentSimplifiedPosition)
                        .CompareTo(tileNavigationController.GetDistanceToCoordinate(positionOfAttackingUnit, unit2.CurrentSimplifiedPosition));
                }

                return unitComparisonValue;
            });
        }

        /// <summary>
        /// Tries to get a walkable tile nearest to an attackable enemy unit.
        /// </summary>
        /// <param name="unit">The unit the maptile should be found for.</param>
        /// <param name="maptTile">The walkable tile closest to the next attackable enemy if available; otherwise null.</param>
        /// <returns>Returns true, when a maptile was found; otherwise false..</returns>
        private bool TryGetWalkableTileClosestToNextAttackableEnemy(BaseUnit unit, out BaseMapTile maptTile)
        {
            maptTile = null;
            if (m_enemyUnits.Count <= 0)
            {
                return false;
            }

            TileNavigationController tileNavigationController = ControllerContainer.TileNavigationController;

            List<BaseMapTile> walkableTiles = tileNavigationController.GetWalkableMapTiles(
                unit.CurrentSimplifiedPosition, new UnitBalancingMovementCostResolver(unit.GetUnitBalancing()));

            if (walkableTiles.Count == 0)
            {
                return false;
            }

            List<BaseUnit> enemiesToCheck = new List<BaseUnit>(m_enemyUnits);
            enemiesToCheck.RemoveAll(enemyUnit => !unit.CanAttackUnit(enemyUnit));

            if (enemiesToCheck.Count == 0)
            {
                return false;
            }

            Vector2 positionOfCurrentUnit = unit.CurrentSimplifiedPosition;

            // Find closest attackable unit
            enemiesToCheck.Sort((unit1, unit2) =>
            {
                int unitRangeComparisonValue = tileNavigationController.GetDistanceToCoordinate(positionOfCurrentUnit, unit1.CurrentSimplifiedPosition)
                    .CompareTo(tileNavigationController.GetDistanceToCoordinate(positionOfCurrentUnit, unit2.CurrentSimplifiedPosition));

                if (unitRangeComparisonValue == 0)
                {
                    unitRangeComparisonValue = Random.Range(-1, 2);
                }

                return unitRangeComparisonValue;
            });

            Vector2 closesEnemyPosition = enemiesToCheck[0].CurrentSimplifiedPosition;

            List<BaseMapTile> mapTilesToAttackFrom = new List<BaseMapTile>();

            // get list of maptiles where closest enemy is attackable from
            foreach (var walkableTile in walkableTiles)
            {
                if (tileNavigationController.GetDistanceToCoordinate(walkableTile.m_SimplifiedMapPosition, 
                    closesEnemyPosition) <= unit.GetUnitBalancing().m_AttackRange)
                {
                    mapTilesToAttackFrom.Add(walkableTile);
                }
            }

            if (mapTilesToAttackFrom.Count > 0)
            {
                // find maptile with max range where the unit can still attack
                mapTilesToAttackFrom.Sort((mapTile1, mapTile2) =>
                {
                    int mapTileDistanceComparison = tileNavigationController.GetDistanceToCoordinate(mapTile2.m_SimplifiedMapPosition, closesEnemyPosition)
                        .CompareTo(tileNavigationController.GetDistanceToCoordinate(mapTile1.m_SimplifiedMapPosition, closesEnemyPosition));

                    return mapTileDistanceComparison;
                });

                maptTile = mapTilesToAttackFrom[0];
            }
            else
            {
                // Find closest walkable (also no unit on it) maptile to attackable unit
                walkableTiles.Sort((mapTile1, mapTile2) =>
                {
                    int mapTileDistanceComparison = tileNavigationController.GetDistanceToCoordinate(mapTile1.m_SimplifiedMapPosition, closesEnemyPosition)
                        .CompareTo(tileNavigationController.GetDistanceToCoordinate(mapTile2.m_SimplifiedMapPosition, closesEnemyPosition));

                    if (mapTileDistanceComparison == 0)
                    {
                        mapTileDistanceComparison = Random.Range(-1, 2);
                    }

                    return mapTileDistanceComparison;
                });

                maptTile = walkableTiles[0];
            }
            
            return true;
        }
    }
}
