using System;
using UnityEngine;

/// <summary>
/// Contains relevant data needed to generate, maintain and control a pool of cloud shadows different for each map.
/// </summary>
[Serializable]
public class MapCloudShadowData
{
    /// <summary>
    /// The size of the maintained cloud shadow instance pool.
    /// </summary>
    public int m_PoolSize;

    /// <summary>
    /// Gets or sets the minimum amout of visible clouds.
    /// </summary>
    public int m_MinVisibleClouds;

    /// <summary>
    /// Gets or sets the maximal possible left spawn position.
    /// </summary>
    public float m_MaxLeftSpawnPosition;

    /// <summary>
    /// Gets or sets the maximum left spawn position.
    /// </summary>
    public float m_MaxRightSpawnPosition;

    /// <summary>
    /// Gets or sets the flying distance of cloud shadows until they're not visible anymore and can be re-used by the pool.
    /// </summary>
    public float m_FlyingDistance;
}
