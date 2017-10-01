using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private string m_levelName;
    private BaseMapTile m_rootMapTile;

    private InputBlocker m_inputBlocker;

    /// <summary>
    /// Sets the name of the level this selector should start.
    /// </summary>
    /// <param name="levelName">Name of the level.</param>
    /// <param name="rootMapTile">The maptile this levelselector lives on.</param>
    public void SetLevelName(string levelName, BaseMapTile rootMapTile)
    {
        m_levelName = levelName;
        m_rootMapTile = rootMapTile;
    }

    /// <summary>
    /// Called when this LevelSelector was selected.
    /// </summary>
    public void OnSelected()
    {
        Debug.Log(string.Format("Selected LevelSelector representing level: {0}", m_levelName));

        // There is always only one unit in the level selection.
        BaseUnit levelSelectionUnit = ControllerContainer.BattleController.RegisteredTeams[TeamColor.Blue][0];

        Dictionary<Vector2, PathfindingNodeDebugData> dontCare;

        var routeToLevelSelector = ControllerContainer.TileNavigationController.GetBestWayToDestination(
            levelSelectionUnit.CurrentSimplifiedPosition, m_rootMapTile.m_SimplifiedMapPosition,
            new LevelSelectionMovementCostResolver(), out dontCare);

        if (m_inputBlocker == null)
        {
            m_inputBlocker = new InputBlocker();
        }

        m_inputBlocker.ChangeBattleControlInput(true);
        //TODO: hide opened level info.
        //TODO: Inject additional action to get maptile type updates while moving to switch visual of unit.
        levelSelectionUnit.MoveAlongRoute(routeToLevelSelector, tile =>
        {
            switch (tile.MapTileType)
            {
                case MapTileType.Grass:
                case MapTileType.Forest:
                    levelSelectionUnit.ChangeVisualsTo(UnitType.BattleTank);
                    break;
                case MapTileType.Water: //TODO: Change to ship once visual are in.
                case MapTileType.Mountain:
                    levelSelectionUnit.ChangeVisualsTo(UnitType.Bomber);
                    break;
            }

        }, () =>
        {
            m_inputBlocker.ChangeBattleControlInput(false);
            //TODO: display level info.
        });
    }
}