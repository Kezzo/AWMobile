using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private string m_levelName;
    private BaseMapTile m_rootMapTile;
    public BaseMapTile RootMapTile { get { return m_rootMapTile; } }

    private InputBlocker m_inputBlocker;

    /// <summary>
    /// Sets the name of the level this selector should start.
    /// </summary>
    /// <param name="levelName">Name of the level.</param>
    /// <param name="orderNumber">The order number of this level selector.</param>
    /// <param name="rootMapTile">The maptile this levelselector lives on.</param>
    public void Initialize(string levelName, int orderNumber, BaseMapTile rootMapTile)
    {
        m_levelName = levelName;
        m_rootMapTile = rootMapTile;

        ControllerContainer.LevelSelectionInitializationController.RegisterLevelSelector(orderNumber, this);
    }

    /// <summary>
    /// Called when this LevelSelector was selected.
    /// </summary>
    public void OnSelected()
    {
        Debug.Log(string.Format("Selected LevelSelector representing level: {0}", m_levelName));

        if (m_inputBlocker == null)
        {
            m_inputBlocker = new InputBlocker();
        }

        // There is always only one unit in the level selection.
        BaseUnit levelSelectionUnit = ControllerContainer.BattleController.RegisteredTeams[TeamColor.Blue][0];

        if (levelSelectionUnit.CurrentSimplifiedPosition == m_rootMapTile.m_SimplifiedMapPosition)
        {
            //TODO: Enter level.
            Debug.Log(string.Format("Entering level: {0}", m_levelName));
            m_inputBlocker.ChangeBattleControlInput(true);

            SwitchToLevel();

            return;
        }

        Debug.Log(string.Format("Moving to level selector of level: {0}", m_levelName));

        Dictionary<Vector2, PathfindingNodeDebugData> dontCare;

        var routeToLevelSelector = ControllerContainer.TileNavigationController.GetBestWayToDestination(
            levelSelectionUnit.CurrentSimplifiedPosition, m_rootMapTile.m_SimplifiedMapPosition,
            new LevelSelectionMovementCostResolver(), out dontCare);

        m_inputBlocker.ChangeBattleControlInput(true, InputBlockMode.SelectionOnly);
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
            m_inputBlocker.ChangeBattleControlInput(false, InputBlockMode.SelectionOnly);
            //TODO: display level info.
        });
    }

    /// <summary>
    /// Draws a map marker route to the given level selector.
    /// It's assumed that a connection exists.
    /// </summary>
    /// <param name="levelSelector">The level selector to draw a route to.</param>
    public void DrawRouteToLevelSelector(LevelSelector levelSelector)
    {
        var navigationController = ControllerContainer.TileNavigationController;
        Dictionary <Vector2, PathfindingNodeDebugData> dontCare;

        var routeToLevelSelector = navigationController.GetBestWayToDestination(
            RootMapTile.m_SimplifiedMapPosition, levelSelector.RootMapTile.m_SimplifiedMapPosition,
            new LevelSelectionMovementCostResolver(), out dontCare);

        var routeMarkerDefinitions = navigationController.GetRouteMarkerDefinitions(routeToLevelSelector);

        foreach (var routeMarkerDefinition in routeMarkerDefinitions)
        {
            navigationController.GetMapTile(routeMarkerDefinition.Key).InstantiateLevelSelectionRoute(routeMarkerDefinition.Value);
        }
    }

    /// <summary>
    /// Switches to the level of this selector.
    /// </summary>
    private void SwitchToLevel()
    {
        Root.Instance.LoadingUi.Show();

        Root.Instance.CoroutineHelper.CallDelayed(Root.Instance, 1.05f, () =>
        {
            Root.Instance.SceneLoading.UnloadExistingScenes(() =>
            {
                Root.Instance.SceneLoading.LoadToLevel(m_levelName, () =>
                {
                    m_inputBlocker.ChangeBattleControlInput(false);
                    Root.Instance.LoadingUi.Hide();
                });
            });
        });
    }
}