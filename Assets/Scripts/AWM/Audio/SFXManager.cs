using System;
using System.Collections.Generic;
using UnityEngine;

namespace AWM.Audio
{
    public class SFXManager : MonoBehaviour
    {
        [Serializable]
        private class SoundEffectReference
        {
            public SoundEffect SoundEffect;
            public AudioClip AudioClip;
            public bool Loop;
            public float Volume = 1f;
        }

        [SerializeField]
        private List<SoundEffectReference> soundEffects;

        [SerializeField]
        private List<AudioSource> sfxAudioSourcePool;

        private int nextPoolIndex;

        public AudioSource PlaySFX(SoundEffect soundEffect)
        {
            SoundEffectReference sfxReference = soundEffects.Find((sfx) => sfx.SoundEffect == soundEffect);

            if (sfxReference == null)
            {
                Debug.LogError("SoundEffect: " + soundEffect + " is not referenced!");
                return null;
            }

            AudioSource audioSourceToUse = sfxAudioSourcePool[nextPoolIndex];
            nextPoolIndex = (nextPoolIndex + 1) % sfxAudioSourcePool.Count;

            audioSourceToUse.clip = sfxReference.AudioClip;
            audioSourceToUse.loop = sfxReference.Loop;
            audioSourceToUse.volume = sfxReference.Volume;
            audioSourceToUse.Play();

            return audioSourceToUse;
        }

        public AudioClip GetClip(SoundEffect soundEffect)
        {
            SoundEffectReference sfxReference = soundEffects.Find((sfx) => sfx.SoundEffect == soundEffect);

            if (sfxReference == null)
            {
                Debug.LogError("SoundEffect: " + soundEffect + " is not referenced!");
                return null;
            }

            return sfxReference.AudioClip;
        }
    }
}