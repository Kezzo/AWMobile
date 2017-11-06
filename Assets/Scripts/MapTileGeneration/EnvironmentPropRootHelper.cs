using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central environment prop mesh combination class that provide specifically setup root objects for each <see cref="EnvironmentPropType"/>.
/// </summary>
public class EnvironmentPropRootHelper : MonoBehaviour
{
    [Serializable]
    public class EnvironmentPropRoot
    {
        public EnvironmentPropType m_EnvironmentPropType;
        public Transform m_RootTransform;
    }

    [SerializeField]
    private List<EnvironmentPropRoot> m_environmentPropRoots;

    /// <summary>
    /// Returns the root of the given <see cref="EnvironmentPropType"/>.
    /// </summary>
    /// <param name="environmentPropType">Type of the environment property.</param>
    public Transform GetEnvironmentPropTypeRoot(EnvironmentPropType environmentPropType)
    {
        return m_environmentPropRoots.Find(root => root.m_EnvironmentPropType == environmentPropType).m_RootTransform;
    }

    /// <summary>
    /// Removes the rendering components from the given gameobject.
    /// </summary>
    /// <param name="gameObject">The gameobject.</param>
    public void RemoveRenderingComponents(GameObject gameObject)
    {
        Destroy(gameObject.GetComponent<MeshRenderer>());
        Destroy(gameObject.GetComponent<MeshFilter>());
    }
}
