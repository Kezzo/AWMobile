﻿using System.Collections;
using System.Collections.Generic;
using AWM.System;
using UnityEngine;

namespace AWM.BattleVisuals
{
    /// <summary>
    /// Represents one instance of a cloud shadow that travels from a start to a destination position with a given speed.
    /// When the cloud reached the destination position, it'll inform the <see cref="CloudShadowOrchestrator"/>, so it can re-used in its pool.
    /// </summary>
    public class CloudShadow : MonoBehaviour
    {
        [SerializeField]
        private MeshFilter m_meshFilter;

        [SerializeField]
        private List<Mesh> m_availableCloudMeshes;

        /// <summary>
        /// The cloud shadow orchestrator this instance was generated by.
        /// Will be informed when the cloud shadow reaches the given destination position.
        /// </summary>
        private CloudShadowOrchestrator m_cloudShadowOrchestrator;

        /// <summary>
        /// Initializes this instance and start moving.
        /// </summary>
        /// <param name="cloudShadowOrchestrator">The cloud shadow orchestrator.</param>
        /// <param name="startPosition">The start position.</param>
        /// <param name="destinationPosition">The destination position.</param>
        /// <param name="speed">The speed.</param>
        public void Initialize(CloudShadowOrchestrator cloudShadowOrchestrator, Vector3 startPosition, Vector3 destinationPosition, float speed)
        {
            m_cloudShadowOrchestrator = cloudShadowOrchestrator;
            m_meshFilter.mesh = m_availableCloudMeshes[Random.Range(0, m_availableCloudMeshes.Count)];
            this.transform.localPosition = startPosition;

            this.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            StartCoroutine(MoveCloud(destinationPosition, speed));
        }

        /// <summary>
        /// Moves this cloud instance from the start to the end position.
        /// </summary>
        /// <param name="destinationPosition">The destination position.</param>
        /// <param name="speed">The speed.</param>
        /// <returns></returns>
        private IEnumerator MoveCloud(Vector3 destinationPosition, float speed)
        {
            while (true)
            {
                while (CC.BSC.IsBattlePaused)
                {
                    yield return null;
                }

                this.transform.localPosition = Vector3.MoveTowards(this.transform.localPosition, destinationPosition, speed);

                if (this.transform.localPosition.z - destinationPosition.z < 0.1f)
                {
                    //Debug.Log(string.Format("Cloud arrived at destination. Current position: {0} Destination: {1}", this.transform.localPosition, destinationPosition));

                    m_cloudShadowOrchestrator.ReAddCloudShadowToPool(this);
                    yield break;
                }

                yield return null;
            }
        }

    }
}
