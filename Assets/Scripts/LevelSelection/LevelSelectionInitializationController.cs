using System.Collections.Generic;

public class LevelSelectionInitializationController
{
    /// <summary>
    /// Stores the registered level selector instances.
    /// </summary>
    private readonly Dictionary<int, LevelSelector> m_registeredLevelSelectors = new Dictionary<int, LevelSelector>();

    /// <summary>
    /// Registers a level selector.
    /// </summary>
    /// <param name="orderNumber">The order number of the level selector.</param>
    /// <param name="levelSelector">The level selector instance.</param>
    public void RegisterLevelSelector(int orderNumber, LevelSelector levelSelector)
    {
        m_registeredLevelSelectors[orderNumber] = levelSelector;
    }

    /// <summary>
    /// Tries to get a level selector based on the given order number.
    /// </summary>
    /// <param name="orderNumber">The order number of the level selector to get.</param>
    /// <param name="levelSelector">The found level selector instance.</param>
    /// <returns>If a level selector with the given order number was found true; otherwise false.</returns>
    public bool TryGetLevelSelector(int orderNumber, out LevelSelector levelSelector)
    {
        return m_registeredLevelSelectors.TryGetValue(orderNumber, out levelSelector);
    }

    /// <summary>
    /// Initializes the level selection visuals, depending on the players progress.
    /// TODO: Inject player progress here.
    /// </summary>
    public void InitializeLevelSelectionVisuals()
    {
        int levelSelectionCounter = 0;

        while (true)
        {
            LevelSelector levelSelectorToInitialize;
            LevelSelector nextLevelSelector;

            // This breaks on the last level selector, because no route has to drawn from it.
            if (!m_registeredLevelSelectors.TryGetValue(levelSelectionCounter, out levelSelectorToInitialize) ||
                !m_registeredLevelSelectors.TryGetValue(levelSelectionCounter + 1, out nextLevelSelector))
            {
                break;
            }

            levelSelectorToInitialize.DrawRouteToLevelSelector(nextLevelSelector);

            levelSelectionCounter++;
        }
    }
}
