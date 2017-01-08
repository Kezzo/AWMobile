using System.Collections.Generic;

public class AIController
{
    private Team m_myTeam;
    public TeamColor MyTeamColor { get { return m_myTeam.m_TeamColor; } }
    private List<BaseUnit> m_myUnits;
    private List<BaseUnit> m_enemyUnits;

    private int unitCounter = 0;

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
        if (unitCounter < m_myUnits.Count)
        {
            BaseMapTile tileToWalkTo = GetWalkableTileNearestToEnemy(m_myUnits[unitCounter]);
            if (tileToWalkTo != null)
            {
                var dontCare = new Dictionary<UnityEngine.Vector2, PathfindingNodeDebugData>();
                m_myUnits[unitCounter].MoveAlongRoute(ControllerContainer.TileNavigationController.GetBestWayToDestination(m_myUnits[unitCounter], tileToWalkTo, out dontCare), AttackIfPossible);
                
            }
            else
            {
                AttackIfPossible();
            }
        }
        else
        {
            unitCounter = 0;
            ControllerContainer.BattleController.EndCurrentTurn();
        }
    }

    /// <summary>
    /// Attacks if possible.
    /// </summary>
    private void AttackIfPossible()
    {
        if (unitCounter < m_myUnits.Count)
        {
            List<UnityEngine.Vector2> adjacentPositions = ControllerContainer.TileNavigationController.GetAdjacentNodes(m_myUnits[unitCounter].CurrentSimplifiedPosition);
            for (int i = 0; i < adjacentPositions.Count; i++)
            {
                if (ControllerContainer.BattleController.IsUnitOnNode(adjacentPositions[i]) && ControllerContainer.BattleController.GetUnitOnNode(adjacentPositions[i]).TeamAffinity.m_TeamColor != m_myTeam.m_TeamColor)
                {
                    m_myUnits[unitCounter].AttackUnit(ControllerContainer.BattleController.GetUnitOnNode(adjacentPositions[i]));
                    //TODO: Do I need another Solution?
                    break;
                }
            }

        }

        unitCounter++;
        MoveNextUnit();
    }

    /// <summary>
    /// Gets the walkable tile nearest to enemy.
    /// </summary>
    /// <param name="unit">The unit.</param>
    /// <returns></returns>
    private BaseMapTile GetWalkableTileNearestToEnemy(BaseUnit unit)
    {
        List<BaseMapTile> walkableTiles = ControllerContainer.TileNavigationController.GetWalkableMapTiles(unit);
        int unitWithLowestDistance = 0;
        int lowestDistanceFound = int.MaxValue;
        for (int i = 0; i < m_enemyUnits.Count; i++)
        {
            int dist = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(unit.CurrentSimplifiedPosition, m_enemyUnits[i].CurrentSimplifiedPosition);
            if (lowestDistanceFound > dist)
            {
                lowestDistanceFound = dist;
                unitWithLowestDistance = i;
            }

        }
        BaseMapTile tileWithLowestDistToEnemy = null;
        lowestDistanceFound = int.MaxValue;
        for (int i = 0; i < walkableTiles.Count; i++)
        {
            int dist = ControllerContainer.TileNavigationController.GetDistanceToCoordinate(walkableTiles[i].SimplifiedMapPosition, m_enemyUnits[unitWithLowestDistance].CurrentSimplifiedPosition);
            if (lowestDistanceFound > dist)
            {
                lowestDistanceFound = dist;
                tileWithLowestDistToEnemy = walkableTiles[i];
            }
        }
        return tileWithLowestDistToEnemy;
    }
}
