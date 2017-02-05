using System.Collections.Generic;
using UnityEngine;

public class AIController
{
    private Team m_myTeam;
    public TeamColor MyTeamColor { get { return m_myTeam.m_TeamColor; } }
    private List<BaseUnit> m_myUnits;
    private List<BaseUnit> m_enemyUnits;

    private int m_unitCounter = 0;

    public AIController(Team aiTeam)
    {
        m_myTeam = aiTeam;
        m_enemyUnits = new List<BaseUnit>();
    }

    /// <summary>
    /// Starts the turn.
    /// </summary>
    public void StartTurn()
    {
        Dictionary<TeamColor, List<BaseUnit>> units = ControllerContainer.BattleController.RegisteredUnits;
        m_myUnits = units[m_myTeam.m_TeamColor];
        m_enemyUnits = new List<BaseUnit>();

        foreach (var item in units)
        {
            if (item.Key != m_myTeam.m_TeamColor)
            {
                m_enemyUnits.AddRange(item.Value);
            }
        }

        MoveNextUnit();
    }

    /// <summary>
    /// Moves the next unit.
    /// </summary>
    private void MoveNextUnit()
    {
        if (m_unitCounter < m_myUnits.Count)
        {
            BaseMapTile tileToWalkTo = GetWalkableTileOfClosestAttackableEnemy(m_myUnits[m_unitCounter]);
            if (tileToWalkTo != null)
            {
                var dontCare = new Dictionary<Vector2, PathfindingNodeDebugData>();
                m_myUnits[m_unitCounter].MoveAlongRoute(ControllerContainer.TileNavigationController.GetBestWayToDestination(m_myUnits[m_unitCounter], tileToWalkTo, out dontCare), AttackIfPossible);

            }
            else
            {
                AttackIfPossible();
            }
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
        List<Vector2> adjacentPositions = ControllerContainer.TileNavigationController.GetAdjacentNodes(m_myUnits[m_unitCounter].CurrentSimplifiedPosition);

        for (int i = 0; i < adjacentPositions.Count; i++)
        {
            BaseUnit unitToCheck = ControllerContainer.BattleController.GetUnitOnNode(adjacentPositions[i]);

            if (unitToCheck != null && m_myUnits[m_unitCounter].CanUnitAttackUnit(unitToCheck))
            {
                m_myUnits[m_unitCounter].AttackUnit(unitToCheck, () =>
                {
                    // Check if unit died
                    if (!ControllerContainer.BattleController.IsUnitOnNode(adjacentPositions[i]))
                    {
                        m_enemyUnits.Remove(unitToCheck);
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
    /// Gets the walkable tile nearest to enemy.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns></returns>
    private BaseMapTile GetWalkableTileOfClosestAttackableEnemy(BaseUnit unit)
    {
        if (m_enemyUnits.Count <= 0)
        {
            return null;
        }

        //TODO: Consider the attack range of units

        List<BaseMapTile> walkableTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(unit);
        int unitWithLowestDistance = 0;
        int lowestDistanceFound = int.MaxValue;

        // Get closest attackable enemy
        for (int i = 0; i < m_enemyUnits.Count; i++)
        {
            if (!unit.CanUnitAttackUnit(m_enemyUnits[i]))
            {
                continue;   
            }

            int dist = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(
                unit.CurrentSimplifiedPosition, m_enemyUnits[i].CurrentSimplifiedPosition);

            if (lowestDistanceFound > dist)
            {
                lowestDistanceFound = dist;
                unitWithLowestDistance = i;
            }

        }

        BaseMapTile tileWithLowestDistToEnemy = null;
        lowestDistanceFound = int.MaxValue;

        // Get closest tile next to unit to attack
        for (int i = 0; i < walkableTiles.Count; i++)
        {
            int dist = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(walkableTiles[i].SimplifiedMapPosition, 
                m_enemyUnits[unitWithLowestDistance].CurrentSimplifiedPosition);

            if (lowestDistanceFound > dist)
            {
                lowestDistanceFound = dist;
                tileWithLowestDistToEnemy = walkableTiles[i];
            }
        }

        return tileWithLowestDistToEnemy;
    }
}
