using System.Collections.Generic;
using AWM.System;
using UnityEngine;

namespace AWM.LevelSelection
{
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
        /// </summary>
        public void InitializeLevelSelectionVisuals()
        {
            int levelSelectionCounter = 0;

            while (true)
            {
                bool lastLevelSelectorReached = false;
                LevelSelector levelSelectorToInitialize;
                LevelSelector nextLevelSelector = null;

                // This breaks on the last level selector, because no route has to drawn from it.
                if (!m_registeredLevelSelectors.TryGetValue(levelSelectionCounter, out levelSelectorToInitialize) ||
                    !m_registeredLevelSelectors.TryGetValue(levelSelectionCounter + 1, out nextLevelSelector))
                {
                    lastLevelSelectorReached = true;
                }

                if (levelSelectorToInitialize == null)
                {
                    Debug.LogError(string.Format("Expected LevelSelector with order number '{0}' not found!", 
                        levelSelectionCounter));
                    break;
                }

                // First level selector should always be active.
                if (levelSelectionCounter == 0)
                {
                    levelSelectorToInitialize.gameObject.SetActive(true);
                }

                if (Application.isPlaying)
                {
                    levelSelectorToInitialize.ValidateLevelSelectionUnitsPosition();
                }

                if (lastLevelSelectorReached)
                {
                    break;
                }

                if (CC.PPS.IsLevelCompleted(levelSelectorToInitialize.LevelName) || !Application.isPlaying)
                {
                    levelSelectorToInitialize.DrawRouteToLevelSelector(nextLevelSelector, 
                        !CC.PPS.IsLevelCompleted(nextLevelSelector.LevelName));
                }

                levelSelectionCounter++;
            }
        }
    }
}
