using System;
using System.Collections.Generic;
using AWM.Enums;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AWM.MapTileGeneration
{
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
            public EnvironmentPropType m_PropType;
            public GameObject m_Prefab;
            public int m_RandomWeight;

            public bool m_RotateXAxis;
            public bool m_RotateYAxis;
            public bool m_RotateZAxis;

            public Vector2 m_ScaleX;
            public Vector2 m_ScaleY;
            public Vector2 m_ScaleZ;
#pragma warning restore 649
        }
    

        [SerializeField]
        private int m_maxRandomRange;

        private Dictionary<EnvironmentPropType, List<InstantiatedEnvironmentProp>> m_currentlyInstantiatedPrefabs;

        private struct InstantiatedEnvironmentProp
        {
            public bool m_HiddenWhenUnitIsOnTile;
            public GameObject m_InstantiatedPrefab;
        }

        /// <summary>
        /// Instantiates the serialized environment prefabs under the defines transform roots,
        /// with random positioning and Y-rotation.
        /// </summary>
        public void InstantiateEnvironment()
        {
            if (m_prefabsToInstantiate.Count == 0)
            {
                return;
            }

            m_currentlyInstantiatedPrefabs = new Dictionary<EnvironmentPropType, List<InstantiatedEnvironmentProp>>();

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

                instantiatedPrefab.transform.localScale = new Vector3(
                    Random.Range(prefabToInstantiate.m_ScaleX.x, prefabToInstantiate.m_ScaleX.y),
                    Random.Range(prefabToInstantiate.m_ScaleY.x, prefabToInstantiate.m_ScaleY.y),
                    Random.Range(prefabToInstantiate.m_ScaleZ.x, prefabToInstantiate.m_ScaleZ.y));

                var instantiatedProp = new InstantiatedEnvironmentProp
                {
                    m_HiddenWhenUnitIsOnTile = m_possiblePlacementPosition[i].m_HideWhenUnitIsOnTile,
                    m_InstantiatedPrefab = instantiatedPrefab
                };

                if (m_currentlyInstantiatedPrefabs.ContainsKey(prefabToInstantiate.m_PropType))
                {
                    m_currentlyInstantiatedPrefabs[prefabToInstantiate.m_PropType].Add(instantiatedProp);
                }
                else
                {
                    m_currentlyInstantiatedPrefabs.Add(prefabToInstantiate.m_PropType, new List<InstantiatedEnvironmentProp> { instantiatedProp });
                }
            }
        }

        /// <summary>
        /// Destroys the instantiated environment props.
        /// </summary>
        public void ClearInstantiatedEnvironment()
        {
            if (m_currentlyInstantiatedPrefabs == null || m_currentlyInstantiatedPrefabs.Count == 0)
            {
                return;
            }

            foreach (var currentlyInstantiatedPrefab in m_currentlyInstantiatedPrefabs)
            {
                for (int i = currentlyInstantiatedPrefab.Value.Count - 1; i >= 0; i--)
                {
#if UNITY_EDITOR
                    DestroyImmediate(currentlyInstantiatedPrefab.Value[i].m_InstantiatedPrefab);
#else
                Destroy(currentlyInstantiatedPrefab.Value[i].m_InstantiatedPrefab);
#endif

                    currentlyInstantiatedPrefab.Value.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Returns the mesh filter components of all generated props by their type as key.
        /// </summary>
        public Dictionary<EnvironmentPropType, List<GameObject>> GetMergeablePropsByType()
        {
            Dictionary <EnvironmentPropType, List<GameObject>> meshFiltersByPropType = 
                new Dictionary<EnvironmentPropType, List<GameObject>>(m_currentlyInstantiatedPrefabs.Count);

            foreach (var currentlyInstantiatedPrefabPair in m_currentlyInstantiatedPrefabs)
            {
                foreach (var environmentProp in currentlyInstantiatedPrefabPair.Value)
                {
                    if (!environmentProp.m_HiddenWhenUnitIsOnTile)
                    {
                        if (!meshFiltersByPropType.ContainsKey(currentlyInstantiatedPrefabPair.Key))
                        {
                            meshFiltersByPropType.Add(currentlyInstantiatedPrefabPair.Key, new List<GameObject>());
                        }

                        meshFiltersByPropType[currentlyInstantiatedPrefabPair.Key].Add(environmentProp.m_InstantiatedPrefab);
                    }
                }
            }

            return meshFiltersByPropType;
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
}
