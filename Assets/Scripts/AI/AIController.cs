using System.Collections.Generic;
using UnityEngine;

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
    private List<BaseUnit> m_myUnits;

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
        get { return this.m_myUnits[this.m_unitCounter]; }
    }

    /// <summary>
    /// Starts the turn.
    /// </summary>
    public void StartTurn()
    {
        Dictionary<TeamColor, List<BaseUnit>> units = ControllerContainer.BattleController.RegisteredUnits;
        m_myUnits = units[this.MyTeamColor];
        m_enemyUnits = new List<BaseUnit>();

        foreach (var item in units)
        {
            if (item.Key != m_myTeam.m_TeamColor)
            {
                m_enemyUnits.AddRange(item.Value);
            }
        }

        m_unitCounter = 0;
        MoveNextUnit();
    }

    /// <summary>
    /// Moves the next unit.
    /// </summary>
    private void MoveNextUnit()
    {
        if (m_unitCounter < m_myUnits.Count)
        {
            // To pause between each unit that was moved
            Root.Instance.CoroutineHelper.CallDelayed(CurrentlyControlledUnit, 0.2f, () =>
            {
                BaseMapTile tileToWalkTo = GetWalkableTileClosestToNextAttackableEnemy(CurrentlyControlledUnit);
                if (tileToWalkTo != null)
                {
                    var dontCare = new Dictionary<Vector2, PathfindingNodeDebugData>();

                    List<Vector2> routeToMove = ControllerContainer.TileNavigationController.GetBestWayToDestination(
                        CurrentlyControlledUnit, tileToWalkTo, out dontCare);

                    CurrentlyControlledUnit.MoveAlongRoute(routeToMove, AttackIfPossible);
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
    /// Gets the walkable tile nearest to an enemy unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The tile closest to an enemy unit.</returns>
    private BaseMapTile GetWalkableTileClosestToNextAttackableEnemy(BaseUnit unit)
    {
        if (m_enemyUnits.Count <= 0)
        {
            return null;
        }

        TileNavigationController tileNavigationController = ControllerContainer.TileNavigationController;

        List<BaseMapTile> walkableTiles = tileNavigationController.GetWalkableMapTiles(unit);

        if (walkableTiles.Count == 0)
        {
            return null;
        }

        List<BaseUnit> enemiesToCheck = new List<BaseUnit>(m_enemyUnits);
        enemiesToCheck.RemoveAll(enemyUnit => !unit.CanAttackUnit(enemyUnit));

        Vector2 positionOfCurrentUnit = unit.CurrentSimplifiedPosition;

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

        walkableTiles.Sort((mapTile1, mapTile2) =>
        {
            int mapTileDistanceComparison = tileNavigationController.GetDistanceToCoordinate(mapTile1.SimplifiedMapPosition, closesEnemyPosition)
                .CompareTo(tileNavigationController.GetDistanceToCoordinate(mapTile2.SimplifiedMapPosition, closesEnemyPosition));

            if (mapTileDistanceComparison == 0)
            {
                mapTileDistanceComparison = Random.Range(-1, 2);
            }

            return mapTileDistanceComparison;
        });

        return walkableTiles[0];
    }
}
