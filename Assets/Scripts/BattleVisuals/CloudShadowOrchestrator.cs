using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates, maintains and controls a pool of cloud instances that generate shadows.
/// </summary>
public class CloudShadowOrchestrator : MonoBehaviour
{
    [SerializeField]
    private int m_cloudPoolSize;

    [SerializeField]
    private CloudShadow m_cloudShadowPrefab;

    [SerializeField]
    private Transform m_spawnRoot;

    private Queue<CloudShadow> m_availableCloudShadows;
    private MapCloudShadowData m_mapCloudShadowData;
    private int m_currentlyVisibleClouds;

    private const float DelayBetweenClouds = 2f;
    private float m_lastCloudStartTime;

    /// <summary>
    /// Generates a cloud pool.
    /// </summary>
    /// <param name="mapCloudShadowData">The map cloud shadow data.</param>
    public void GenerateCloudPool(MapCloudShadowData mapCloudShadowData)
    {
        m_mapCloudShadowData = mapCloudShadowData;
        m_availableCloudShadows = new Queue<CloudShadow>(mapCloudShadowData.m_PoolSize);

        for (int i = 0; i < mapCloudShadowData.m_PoolSize; i++)
        {
            CloudShadow instantiatedInstance = Instantiate(m_cloudShadowPrefab);
            instantiatedInstance.transform.SetParent(m_spawnRoot);
            instantiatedInstance.transform.localScale = new Vector3(2f, 2f, 1f);
            instantiatedInstance.gameObject.SetActive(false);

            m_availableCloudShadows.Enqueue(instantiatedInstance);
        }
    }

    /// <summary>
    /// Starts the display cloud shadows.
    /// </summary>
    public void StartCloudShadowDisplay()
    {
        PreHeatCloudShadowPositions();
        StartCoroutine(UpdateCloudShadowPositions());
    }

    /// <summary>
    /// Updates the cloud shadow positions.
    /// </summary>
    private IEnumerator UpdateCloudShadowPositions()
    {
        while (true)
        {
            if (m_mapCloudShadowData.m_MinVisibleClouds > m_currentlyVisibleClouds &&
                DelayBetweenClouds < Time.realtimeSinceStartup - m_lastCloudStartTime)
            {
                Vector3 startPosition = Vector3.Lerp(m_mapCloudShadowData.m_MaxLeftSpawnPosition, m_mapCloudShadowData.m_MaxRightSpawnPosition,
                Random.Range(0f, 1f));

                Vector3 destinationPosition = new Vector3(startPosition.x,
                    startPosition.y, startPosition.z - m_mapCloudShadowData.m_FlyingDistance);

                CloudShadow cloudShadow = m_availableCloudShadows.Dequeue();
                cloudShadow.gameObject.SetActive(true);
                cloudShadow.Initialize(this, startPosition, destinationPosition, Random.Range(0.01f, 0.05f));

                m_lastCloudStartTime = Time.realtimeSinceStartup;
                m_currentlyVisibleClouds++;
            }

            yield return null;
        }
    }

    /// <summary>
    /// Pre-heats cloud shadow positions.
    /// So cloud shadow are all over the place already the moment the user sees the map.
    /// </summary>
    private void PreHeatCloudShadowPositions()
    {
        
    }

    /// <summary>
    /// Re-adds a cloud shadow to the pool of available instances.
    /// </summary>
    /// <param name="cloudShadow">The cloud shadow instance.</param>
    public void ReAddCloudShadowToPool(CloudShadow cloudShadow)
    {
        cloudShadow.gameObject.SetActive(false);
        m_availableCloudShadows.Enqueue(cloudShadow);
        m_currentlyVisibleClouds--;
    }
}
