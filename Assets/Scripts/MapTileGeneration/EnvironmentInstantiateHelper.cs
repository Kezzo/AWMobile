using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Helper class to instantiate defined prefabs under defines transform roots.
/// </summary>
public class EnvironmentInstantiateHelper : MonoBehaviour
{
    [SerializeField]
    private List<Transform> m_possiblePlacementPosition;

    [SerializeField]
    private List<GameObject> m_prefabsToInstantiate;

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

            GameObject prefabToInstantiate = m_prefabsToInstantiate[Random.Range(0, m_prefabsToInstantiate.Count)];

            if (prefabToInstantiate == null)
            {
                Debug.LogError("Randomly chosen prefab to instantiate was null!");
                continue;
            }

            GameObject instantiatedPrefab = Instantiate(prefabToInstantiate, m_possiblePlacementPosition[i]);
            instantiatedPrefab.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
    }
}
