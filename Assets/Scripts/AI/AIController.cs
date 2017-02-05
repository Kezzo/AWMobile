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
    private Team m_myTeam;

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
        this.m_myTeam = aiTeam;
        this.m_enemyUnits = new List<BaseUnit>();
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
        Dictionary<TeamColor, List<BaseUnit>> units = ControllerContainer.BattleController.RegisteredUnits;
        this.m_myUnits = units[this.MyTeamColor];
        this.m_enemyUnits = new List<BaseUnit>();

        foreach (var item in units)
        {
            if (item.Key != this.m_myTeam.m_TeamColor)
            {
                this.m_enemyUnits.AddRange(item.Value);
            }
        }

        this.m_unitCounter = 0;
        this.MoveNextUnit();
    }

    /// <summary>
    /// Moves the next unit.
    /// </summary>
    private void MoveNextUnit()
    {
        if (this.m_unitCounter < this.m_myUnits.Count)
        {
            BaseMapTile tileToWalkTo = this.GetWalkableTileClosestToNextAttackableEnemy(this.m_myUnits[this.m_unitCounter]);
            if (tileToWalkTo != null)
            {
                var dontCare = new Dictionary<Vector2, PathfindingNodeDebugData>();
                this.m_myUnits[this.m_unitCounter].MoveAlongRoute(ControllerContainer.TileNavigationController.GetBestWayToDestination(this.m_myUnits[this.m_unitCounter], tileToWalkTo, out dontCare), this.AttackIfPossible);
            }
            else
            {
                this.AttackIfPossible();
            }
        }
        else
        {
            this.m_unitCounter = 0;
            ControllerContainer.BattleController.EndCurrentTurn();
        }
    }

    /// <summary>
    /// Attacks if possible.
    /// </summary>
    private void AttackIfPossible()
    {
        BaseUnit attackingUnit = this.m_myUnits[this.m_unitCounter];
        List<BaseUnit> unitsInRange =
            ControllerContainer.BattleController.GetUnitsInRange(attackingUnit.CurrentSimplifiedPosition, attackingUnit.GetUnitBalancing().m_AttackRange);
        for (int i = 0; i < unitsInRange.Count; i++)
        {
            if (attackingUnit.CanUnitAttackUnit(unitsInRange[i]))
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

                            this.m_unitCounter++;
                            this.MoveNextUnit();
                        });

                return;
            }
        }

        this.m_unitCounter++;
        this.MoveNextUnit();
    }

    /// <summary>
    /// Gets the walkable tile nearest to an enemy unit.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns>The tile closest to an enemy unit.</returns>
    private BaseMapTile GetWalkableTileClosestToNextAttackableEnemy(BaseUnit unit)
    {
        if (this.m_enemyUnits.Count <= 0)
        {
            return null;
        }

        List<BaseMapTile> walkableTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(unit);
        int unitWithLowestDistance = 0;
        int lowestDistanceFound = int.MaxValue;

        // Get closest attackable enemy
        for (int i = 0; i < this.m_enemyUnits.Count; i++)
        {
            if (!unit.CanUnitAttackUnit(this.m_enemyUnits[i]))
            {
                continue;
            }

            int dist = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                unit.CurrentSimplifiedPosition, this.m_enemyUnits[i].CurrentSimplifiedPosition);

            if (lowestDistanceFound > dist)
            {
                lowestDistanceFound = dist;
                unitWithLowestDistance = i;
            }
        }

        if (lowestDistanceFound <= unit.GetUnitBalancing().m_AttackRange)
        {
            return null;
        }

        BaseMapTile tileWithLowestDistToEnemy = null;
        lowestDistanceFound = int.MaxValue;

        // Get closest tile next to unit to attack
        for (int i = 0; i < walkableTiles.Count; i++)
        {
            int dist = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                walkableTiles[i].SimplifiedMapPosition, this.m_enemyUnits[unitWithLowestDistance].CurrentSimplifiedPosition);

            if (lowestDistanceFound > dist)
            {
                lowestDistanceFound = dist;
                if (lowestDistanceFound <= unit.GetUnitBalancing().m_AttackRange)
                {
                    return walkableTiles[i];
                }

                tileWithLowestDistToEnemy = walkableTiles[i];
            }
        }

        return tileWithLowestDistToEnemy;
    }
}
