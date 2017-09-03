using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelector : MonoBehaviour
{
    private string m_levelName;
    private BaseMapTile m_rootMapTile;

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

        BaseUnit levelSelectionUnit = ControllerContainer.BattleController.RegisteredTeams[TeamColor.Blue][0];

        StartCoroutine(MoveToThisSelector(levelSelectionUnit, new List<Vector2>
        {
            new Vector2(14, 1),
            new Vector2(13, 1),
            new Vector2(13, 2),
            new Vector2(13, 3),
            new Vector2(13, 4),
            new Vector2(14, 4)
        }));
    }

    /// <summary>
    /// Moves to this selector.
    /// </summary>
    /// <param name="levelSelectionUnit">The level selection unit.</param>
    /// <param name="route">The route to take.</param>
    private IEnumerator MoveToThisSelector(BaseUnit levelSelectionUnit, List<Vector2> route)
    {
        Vector3 startPosition = levelSelectionUnit.transform.position;
        Vector3 endPosition = ControllerContainer.TileNavigationController.GetMapTile(route[route.Count - 1]).UnitRoot.position;

        // Starting with an index of 1 here, because the node at index 0 is the node the unit is standing on.
        for (int nodeIndex = 1; nodeIndex < route.Count; nodeIndex++)
        {
            Vector2 nodeToMoveTo = route[nodeIndex];
            Vector2 currentNode = route[nodeIndex - 1];

            yield return levelSelectionUnit.MoveToNeighborNode(currentNode, nodeToMoveTo, (endPosition - startPosition).magnitude, endPosition);

            BaseMapTile currentMapTile = ControllerContainer.TileNavigationController
                    .GetMapTile(nodeToMoveTo);

            // Change unit visual depending on maptile. ground => Tank, water => ship, mountain => plane
        }
    }
}