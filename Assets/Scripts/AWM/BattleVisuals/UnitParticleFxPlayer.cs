using System;
using System.Collections.Generic;
using AWM.Enums;
using AWM.System;
using UnityEngine;

namespace AWM.BattleVisuals
{
    public class UnitParticleFxPlayer : MonoBehaviour
    {
#pragma warning disable 649

        [SerializeField]
        private List<ParticleMapping> m_unitParticleFxMappings;

        [Serializable]
        private class ParticleMapping
        {
            public UnitParticleFx m_UnitParticleFx;
            public List<GameObject> m_ParticleSystems;
            public int m_CountToPool;
        }

#pragma warning restore 649

        private Dictionary<UnitParticleFx, KeyValuePair<int, ParticleSystem[][]>> m_particleSystemsPool;

        private void Awake()
        {
            if (!Root.Instance.SceneLoading.IsInLevelSelection)
            {
                InstantiatePool();
                CC.MBR.Register(this);
            }
        }

        /// <summary>
        /// Instantiates the particle effect pool based on the set inspector values.
        /// </summary>
        private void InstantiatePool()
        {
            m_particleSystemsPool = new Dictionary<UnitParticleFx, KeyValuePair<int, ParticleSystem[][]>>();

            for (int i = 0; i < m_unitParticleFxMappings.Count; i++)
            {
                ParticleMapping particleMapping = m_unitParticleFxMappings[i];
                KeyValuePair<int, ParticleSystem[][]> particlePool = new KeyValuePair<int, 
                    ParticleSystem[][]>(0, new ParticleSystem[particleMapping.m_CountToPool][]);

                for (int j = 0; j < particleMapping.m_CountToPool; j++)
                {
                    particlePool.Value[j] = new ParticleSystem[particleMapping.m_ParticleSystems.Count];

                    for (int k = 0; k < particleMapping.m_ParticleSystems.Count; k++)
                    {
                        GameObject instantiatedParticleSystemPrefab = Instantiate(particleMapping.m_ParticleSystems[k]);

                        instantiatedParticleSystemPrefab.transform.SetParent(this.transform);
                        instantiatedParticleSystemPrefab.transform.localPosition = Vector3.zero;
                        instantiatedParticleSystemPrefab.transform.rotation = Quaternion.identity;

                        particlePool.Value[j][k] = instantiatedParticleSystemPrefab.GetComponent<ParticleSystem>();
                    }
                }

                m_particleSystemsPool.Add(particleMapping.m_UnitParticleFx,
                    particlePool);
            }
        }

        /// <summary>
        /// Plays the PFXs depending on the given UnitParticleFx enum at the given position.
        /// </summary>
        /// <param name="particleFxToPlay">The particle fx to play.</param>
        /// <param name="postionToSpawnPfx">The postion to spawn PFX.</param>
        public void PlayPfxAt(UnitParticleFx particleFxToPlay, Vector3 postionToSpawnPfx)
        {
            ParticleMapping particleMapping =
                m_unitParticleFxMappings.Find(mapping => mapping.m_UnitParticleFx == particleFxToPlay);

            if (particleMapping == null)
            {
                Debug.LogError(string.Format("Unable to find particle systems mapping for '{0}'", particleFxToPlay));
                return;
            }

            KeyValuePair<int, ParticleSystem[][]> particlePoolToUse;

            if (!m_particleSystemsPool.TryGetValue(particleFxToPlay, out particlePoolToUse))
            {
                Debug.LogError(string.Format("Unable to find particle pool for '{0}'", particleFxToPlay));
                return;
            }

            for (int i = 0; i < particlePoolToUse.Value[particlePoolToUse.Key].Length; i++)
            {
                particlePoolToUse.Value[particlePoolToUse.Key][i].gameObject.transform.SetPositionAndRotation(postionToSpawnPfx, Quaternion.identity);
                particlePoolToUse.Value[particlePoolToUse.Key][i].Play();
            }

            m_particleSystemsPool[particleFxToPlay] = new KeyValuePair<int, ParticleSystem[][]>(
                particlePoolToUse.Key >= particlePoolToUse.Value.Length - 1 ? 0 : particlePoolToUse.Key + 1, 
                particlePoolToUse.Value);
        }
    }
}

