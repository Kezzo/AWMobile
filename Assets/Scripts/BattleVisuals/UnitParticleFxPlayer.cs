using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitParticleFxPlayer : MonoBehaviour
{
    //TODO: Handle different pfx position for different unit types.

    [SerializeField]
    private List<ParticleMapping> m_unitParticleFxMappings;

    [Serializable]
    private class ParticleMapping
    {
#pragma warning disable 649
        public UnitParticleFx m_UnitParticleFx;
        public List<ParticleSystem> m_ParticleSystems;
#pragma warning restore 649
    }

    /// <summary>
    /// Plays the PFXs depending on the given UnitParticleFx enum.
    /// </summary>
    public void PlayPfx(UnitParticleFx particleFxToPlay)
    {
        ParticleMapping particleMapping =
            m_unitParticleFxMappings.Find(mapping => mapping.m_UnitParticleFx == particleFxToPlay);

        if (particleMapping == null)
        {
            Debug.LogError(string.Format("Unable to find particle systems to play for '{0}'", particleFxToPlay));
        }
        else
        {
            for (int i = 0; i < particleMapping.m_ParticleSystems.Count; i++)
            {
                particleMapping.m_ParticleSystems[i].Play();
            }
        }
    }
}

