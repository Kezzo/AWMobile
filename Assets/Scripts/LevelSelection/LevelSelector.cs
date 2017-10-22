using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private string m_levelName;
    public string LevelName { get { return m_levelName; } }

    private BaseMapTile m_rootMapTile;
    public BaseMapTile RootMapTile { get { return m_rootMapTile; } }

    private BaseUnit m_levelSelectionUnit;
    private BaseUnit LevelSelectionUnit
    {
        get
        {
            // Lazily get and store level selection unit.
            return m_levelSelectionUnit ?? 
                (m_levelSelectionUnit = ControllerContainer.BattleController.RegisteredTeams[TeamColor.Blue][0]);
        }
    }

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

        // There is always only one unit in the level selection.
        BaseUnit levelSelectionUnit = LevelSelectionUnit;

        if (LevelSelectionUnit.CurrentSimplifiedPosition == m_rootMapTile.m_SimplifiedMapPosition)
        {
            //TODO: Enter level.
            Debug.Log(string.Format("Entering level: {0}", m_levelName));
            ControllerContainer.InputBlocker.ChangeBattleControlInput(true);

            SwitchToLevel();

            return;
        }

        Debug.Log(string.Format("Moving to level selector of level: {0}", m_levelName));

        Dictionary<Vector2, PathfindingNodeDebugData> dontCare;

        var routeToLevelSelector = ControllerContainer.TileNavigationController.GetBestWayToDestination(
            levelSelectionUnit.CurrentSimplifiedPosition, m_rootMapTile.m_SimplifiedMapPosition,
            new LevelSelectionMovementCostResolver(), out dontCare);

        ControllerContainer.InputBlocker.ChangeBattleControlInput(true, InputBlockMode.SelectionOnly);
        //TODO: hide opened level info.
        //TODO: Inject additional action to get maptile type updates while moving to switch visual of unit.
        levelSelectionUnit.MoveAlongRoute(routeToLevelSelector, tile =>
        {
            UpdateUnitVisuals(tile.MapTileType);

        }, () =>
        {
            ControllerContainer.InputBlocker.ChangeBattleControlInput(false, InputBlockMode.SelectionOnly);
            //TODO: display level info.
        });
    }

    /// <summary>
    /// Updates the visuals of level selection unit, based on the given <see cref="MapTileType"/>.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    private void UpdateUnitVisuals(MapTileType mapTileType)
    {
        switch (mapTileType)
        {
            case MapTileType.Grass:
            case MapTileType.Forest:
                LevelSelectionUnit.ChangeVisualsTo(UnitType.BattleTank);
                break;
            case MapTileType.Water:
                LevelSelectionUnit.ChangeVisualsTo(UnitType.WarShip);
                break;
            case MapTileType.Mountain:
                LevelSelectionUnit.ChangeVisualsTo(UnitType.Bomber);
                break;
        }
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
    /// Checks the level played level. When this level selector is representing that level, 
    /// it'll position the level selection unit on it.
    /// </summary>
    public void ValidateLevelSelectionUnitsPosition()
    {
        if (m_levelName.Equals(ControllerContainer.PlayerProgressionService.LastPlayedLevel))
        {
            LevelSelectionUnit.SetPositionTo(m_rootMapTile);
            UpdateUnitVisuals(m_rootMapTile.MapTileType);
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
                    ControllerContainer.PlayerProgressionService.LastPlayedLevel = m_levelName;
                    ControllerContainer.InputBlocker.ChangeBattleControlInput(false);
                    Root.Instance.LoadingUi.Hide();
                });
            });
        });
    }
}