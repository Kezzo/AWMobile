using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Helper class to instantiate defined prefabs under defines transform roots.
/// </summary>
public class EnvironmentInstantiateHelper : MonoBehaviour
{
    [SerializeField]
    private List<EnvironmentPlacementSettings> m_possiblePlacementPosition;

    [Serializable]
    private class EnvironmentPlacementSettings
    {
#pragma warning disable 649
        public Transform m_PlacementRoot;
        public bool m_HideWhenUnitIsOnTile;
#pragma warning restore 649
    }

    [SerializeField]
    private List<EnvironmentInstantiateSettings> m_prefabsToInstantiate;

    [Serializable]
    private class EnvironmentInstantiateSettings
    {
#pragma warning disable 649
        public GameObject m_Prefab;
        public int m_RandomWeight;

        public bool m_RotateXAxis;
        public bool m_RotateYAxis;
        public bool m_RotateZAxis;
#pragma warning restore 649
    }

    [SerializeField]
    private int m_maxRandomRange;

    /// <summary>
    /// Instantiates the serialized environment prefabs under the defines transform roots,
    /// with random positioning and Y-rotation.
    /// </summary>
    public void InstantiateEnvironment()
    {
        for (int i = 0; i < m_possiblePlacementPosition.Count; i++)
        {
            if (Random.Range(0, m_maxRandomRange) < 1)
            {
                continue;
            }

            int randomValue = Random.Range(0, GetMaxRollWeight(m_prefabsToInstantiate));
            EnvironmentInstantiateSettings prefabToInstantiate = null;

            ShuffleList(ref m_prefabsToInstantiate);

            int checkedWeight = 0;
            for (int j = 0; j < m_prefabsToInstantiate.Count; j++)
            {
                if (randomValue >= checkedWeight && randomValue < checkedWeight + m_prefabsToInstantiate[j].m_RandomWeight)
                {
                    prefabToInstantiate = m_prefabsToInstantiate[j];
                    break;
                }

                checkedWeight += m_prefabsToInstantiate[j].m_RandomWeight;
            }

            if (prefabToInstantiate == null)
            {
                Debug.LogError("Randomly chosen prefab to instantiate was null!");
                continue;
            }

            GameObject instantiatedPrefab = Instantiate(prefabToInstantiate.m_Prefab, m_possiblePlacementPosition[i].m_PlacementRoot);

            instantiatedPrefab.transform.localRotation = Quaternion.Euler(
                prefabToInstantiate.m_RotateXAxis ? Random.Range(0f, 360f) : 0f, 
                prefabToInstantiate.m_RotateYAxis ? Random.Range(0f, 360f) : 0f, 
                prefabToInstantiate.m_RotateZAxis ? Random.Range(0f, 360f) : 0f);
        }
    }

    /// <summary>
    /// Gets the maximum roll weight based on the given weight values of the objects in the list.
    /// </summary>
    /// <param name="listToGetTheWeightFrom">The list to get the weight from.</param>
    /// <returns></returns>
    private int GetMaxRollWeight(List<EnvironmentInstantiateSettings> listToGetTheWeightFrom)
    {
        int maxRollWeight = 0;

        for (int i = 0; i < listToGetTheWeightFrom.Count; i++)
        {
            maxRollWeight += listToGetTheWeightFrom[i].m_RandomWeight;
        }

        return maxRollWeight;
    }

    /// <summary>
    /// Shuffles a given list.
    /// </summary>
    /// <param name="listToShuffle">The list to shuffle.</param>
    private void ShuffleList<T>(ref List<T> listToShuffle) where T : class
    {
        int n = listToShuffle.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = listToShuffle[k];
            listToShuffle[k] = listToShuffle[n];
            listToShuffle[n] = value;
        }
    }

    /// <summary>
    /// Changes the visibility of the instantiated environment props.
    /// </summary>
    /// <param name="unitIsOnTile">if set to <c>true</c> a unit is on the tile this <see cref="EnvironmentInstantiateHelper"/> is used on.</param>
    public void UpdateVisibilityOfEnvironment(bool unitIsOnTile)
    {
        foreach (var environmentPlacementSetting in m_possiblePlacementPosition)
        {
            bool hideEnvironmentProp = unitIsOnTile && environmentPlacementSetting.m_HideWhenUnitIsOnTile;
            environmentPlacementSetting.m_PlacementRoot.gameObject.SetActive(!hideEnvironmentProp);
        }
    }
}
