using System;
using System.Collections.Generic;
using AWM.System;
using UnityEngine;

namespace AWM.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField]
        private AudioSource musicAudioSource;

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
        private PlayerPrefsStorageHelper playerPrefsHelper;
        public bool MusicIsOn { get; private set; }
        public bool SFXIsOn { get; private set; }
        public bool sfxIsPaused = false;

        private void Awake()
        {
            playerPrefsHelper = new PlayerPrefsStorageHelper();

            string musicSetting = playerPrefsHelper.GetData(StorageKey.MusicSetting);
            MusicIsOn = string.IsNullOrEmpty(musicSetting) || musicSetting == "On";
            SetMusicSetting(MusicIsOn, false);

            string sfxSetting = playerPrefsHelper.GetData(StorageKey.SFXSetting);
            SFXIsOn = string.IsNullOrEmpty(sfxSetting) || sfxSetting == "On";
            SetSFXSetting(SFXIsOn, false);
        }

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

            if(sfxIsPaused)
            {
                audioSourceToUse.Pause();
            }

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

        public bool ToggleMusic(bool storeToPlayerPrefs = true)
        {
            MusicIsOn = !MusicIsOn;
            SetMusicSetting(MusicIsOn, storeToPlayerPrefs);

            return MusicIsOn;
        }

        public void ToggleSfxPause(bool pause)
        {
            sfxIsPaused = pause;
            foreach (var sfxAudioSource in sfxAudioSourcePool)
            {
                if(pause)
                {
                    sfxAudioSource.Pause();
                }
                else
                {
                    sfxAudioSource.UnPause();
                }
            }
        }

        private void SetMusicSetting(bool isOn, bool storeToPlayerPrefs)
        {
            musicAudioSource.mute = !isOn;

            if (storeToPlayerPrefs)
            {
                playerPrefsHelper.StoreData(StorageKey.MusicSetting, isOn ? "On" : "Off");
            }
        }

        public bool ToggleSFX(bool storeToPlayerPrefs = true)
        {
            SFXIsOn = !SFXIsOn;
            SetSFXSetting(SFXIsOn, storeToPlayerPrefs);

            return SFXIsOn;
        }

        private void SetSFXSetting(bool isOn, bool storeToPlayerPrefs)
        {
            foreach (var audioSource in sfxAudioSourcePool)
            {
                audioSource.mute = !isOn;
            }

            if (storeToPlayerPrefs)
            {
                playerPrefsHelper.StoreData(StorageKey.SFXSetting, isOn ? "On" : "Off");
            }
        }
    }
}