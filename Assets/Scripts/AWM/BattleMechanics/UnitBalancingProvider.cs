using System;
using System.Collections.Generic;
using AWM.Enums;
using AWM.Models;
using AWM.System;
using UnityEngine;

namespace AWM.BattleMechanics
{
    public class UnitBalancingProvider
    {
        private Dictionary<UnitType, UnitBalancingData> m_unitBalancingDictionary;

        /// <summary>
        /// Initializes the balancing data.
        /// </summary>
        public void InitializeBalancingData()
        {
            m_unitBalancingDictionary = new Dictionary<UnitType, UnitBalancingData>();

            Array unitTypes = Enum.GetValues(typeof(UnitType));

            foreach (UnitType unitType in unitTypes)
            {
                if (unitType == UnitType.None || m_unitBalancingDictionary.ContainsKey(unitType))
                {
                    continue;
                }

                UnitBalancingData unitBalancingData = CC.ADS.GetAssetDataAtPath<UnitBalancingData>(string.Format(
                    "Balancing/{0}", unitType));

                if (unitBalancingData != null)
                {
                    m_unitBalancingDictionary[unitType] = unitBalancingData;
                }
            }
        }

        /// <summary>
        /// Gets a unit balancing based on the UnitType.
        /// </summary>
        /// <param name="unitType">Type of the unit.</param>
        /// <returns></returns>
        public UnitBalancingData GetUnitBalancing(UnitType unitType)
        {
            if (m_unitBalancingDictionary == null)
            {
                Debug.LogError("Unit balancing dictionary is empty! Call InitializeBalancingData before trying to get unit balancing.");
                return null;
            }

            UnitBalancingData unitBalancingToReturn = null;

            if (!m_unitBalancingDictionary.TryGetValue(unitType, out unitBalancingToReturn))
            {
                Debug.LogErrorFormat("UnitBalancing for UnitType: '{0}' was not found!", unitType);
            }

            return unitBalancingToReturn;
        }
    }
}
