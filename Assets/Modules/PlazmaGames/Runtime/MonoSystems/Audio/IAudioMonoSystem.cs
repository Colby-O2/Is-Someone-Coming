using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlazmaGames.Core.MonoSystem;

namespace PlazmaGames.Audio
{
    public interface IAudioMonoSystem : IMonoSystem
    {
        /// <summary>
        /// Sets the overall volume level
        /// </summary>
        public void SetOverallVolume(float volume);

        /// <summary>
        /// Sets the SFX volume level
        /// </summary>
        public void SetSfXVolume(float volume);

        /// <summary>
        /// Sets the SFX volume level
        /// </summary>
        public void SetAmbientVolume(float volume);

        /// <summary>
        /// Sets the music volume level
        /// </summary>
        public void SetMusicVolume(float volume);

        /// <summary>
        /// Gets the overall volume level
        /// </summary>
        public float GetOverallVolume();

        /// <summary>
        /// Gets the SFX volume level
        /// </summary>
        public float GetSfXVolume();

        /// <summary>
        /// Gets the ambient volume level
        /// </summary>
        public float GetAmbientVolume();

        /// <summary>
        /// Gets the music volume level
        /// </summary>
        public float GetMusicVolume();

        public void PlayAudio(AudioClip clip, AudioType audioType, bool loop = true, bool allowOverlay = true);

        public void PlayAudio(string name, AudioType audioType, bool loop = true, bool allowOverlay = true);

        public void PlayAudio(int id, AudioType audioType, bool loop = true, bool allowOverlay = true);

        public void StopAudio(AudioType audioType);

        public bool IsPlaying(AudioType audioType);
    }
}
