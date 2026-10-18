using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;
using PlazmaGames.DataPersistence;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlazmaGames.Audio
{
    [System.Serializable]
    internal class AudioFile
    {
        public int id;
        public string name;
        public AudioClip audio;
    }

    [System.Serializable]
    internal class AudioSourceInfo
    {
        public float vol;
        public AudioSource src;

        public override bool Equals(object obj)
        {
            if(obj == null || obj is not  AudioSourceInfo) return false;
            AudioSourceInfo other = obj as AudioSourceInfo;
            if(other == null) return false;
            return src.Equals(other.src);
        }

        public override int GetHashCode()
        {
            return src.GetHashCode();
        }
    }

    public sealed class AudioMonoSystem : MonoBehaviour, IAudioMonoSystem, IDataPersistence
    {
        [Header("Settings")]
        [Range(0f, 1f)] [SerializeField] private float _overallSound = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float _ambientSound = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float _sfxSound = 1.0f;
        [Range(0f, 1f)][SerializeField] private float _musicSound = 1.0f;

        [Header("Audio Clips/Sources")]
        [SerializeField] private List<AudioFile> _musicSounds;
        [SerializeField] private List<AudioFile> _ambientSounds;
        [SerializeField] private List<AudioFile> _sfxSounds;

        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambientSource;
        [SerializeField] private AudioSource _sfxMainSource;

        [SerializeField] private List<AudioSourceInfo> _sfxSources;
        [SerializeField] private List<AudioSourceInfo> _musicSources;

        [SerializeField, ReadOnly] private bool _hasInitialized;

        /// <summary>   
        /// Private member that plays music given an audioclip and sound level between 0 and 1. 
        /// Music is play to compleation and only one audio clip can be played at once.
        /// </summary>
        private void PlayMusic(AudioClip audio, float sound)
        {
            if (_musicSource == null) return;
            _musicSource.volume = sound;
            _musicSource.clip = audio;
            _musicSource.Play();
        }

        /// <summary>   
        /// Private member that plays SfX sound given an audioclip and sound level between 0 and 1.
        /// Multiple audio files can be played at once. 
        /// </summary>
        private void PlayAmbient(AudioClip audio, float sound)
        {
            if (_ambientSource == null) return;
            _ambientSource.volume = sound;
            _ambientSource.PlayOneShot(audio);
        }

        private void PlayAmbientLoop(AudioClip audio, float sound)
        {
            if (_ambientSource == null) return;
            _ambientSource.volume = sound;
            _ambientSource.clip = audio;
            _ambientSource.loop = true;
            _ambientSource.Play();
        }

        private void PlaySfxLoop(AudioClip audio, float sound)
        {
            if (_sfxMainSource == null) return;
            _sfxMainSource.volume = sound;
            _sfxMainSource.clip = audio;
            _sfxMainSource.loop = true;
            _sfxMainSource.Play();
        }


        /// <summary>   
        /// Private member that plays SfX sound given an audioclip and sound level between 0 and 1.
        /// Multiple audio files can be played at once. 
        /// </summary>
        private void PlaySfx(AudioClip audio, float sound)
        {
            if (_sfxMainSource == null) return;
            _sfxMainSource.volume = sound;
            _sfxMainSource.PlayOneShot(audio);
        }

        /// <summary>   
        /// Finds and plays music with a given name if such an Audio file exist.
        /// </summary>
        private void PlayMusic(string name)
        {
            AudioFile music = _musicSounds.Find(e => name.CompareTo(e.name) == 1);
            if (music == null) return;
            PlayMusic(music.audio, _overallSound * _musicSound);
        }

        /// <summary>   
        /// Finds and plays music with a given an id if such an Audio file exist.
        /// </summary>
        private void PlayMusic(int id)
        {
            AudioFile music = _musicSounds.Find(e => id == e.id);
            if (music == null) return;
            PlayMusic(music.audio, _overallSound * _musicSound);
        }

        /// <summary>   
        /// Finds and plays a SfX sound with a given name if such an Audio file exist.
        /// </summary>
        private void PlayAmbient(string name, bool allowOverlay = true, bool loop = true)
        {
            AudioFile sfx = _ambientSounds.Find(e => name.CompareTo(e.name) == 0);
            if (sfx == null) return;
            if (!(!allowOverlay && _ambientSource.isPlaying))
            {
                if (!loop) PlayAmbient(sfx.audio, _overallSound * _ambientSound);
                else PlayAmbientLoop(sfx.audio, _overallSound * _ambientSound);
            }
        }

        /// <summary>   
        /// Finds and plays a SfX sound with a given an id if such an Audio file exist.
        /// </summary>
        private void PlayAmbient(int id, bool allowOverlay = true, bool loop = true)
        {
            AudioFile sfx = _ambientSounds.Find(e => id == e.id);
            if (sfx == null) return;
            if (!(!allowOverlay && _ambientSource.isPlaying))
            {
                if (!loop) PlayAmbient(sfx.audio, _overallSound * _ambientSound);
                else PlayAmbientLoop(sfx.audio, _overallSound * _ambientSound);
            }
        }

        /// <summary>   
        /// Finds and plays a SfX sound with a given name if such an Audio file exist.
        /// </summary>
        private void PlaySfx(string name, bool allowOverlay = true, bool loop = true)
        {
            AudioFile sfx = _sfxSounds.Find(e => name.CompareTo(e.name) == 0);
            if (sfx == null) return;
            if (!(!allowOverlay && _sfxMainSource.isPlaying))
            {
                if (!loop) PlaySfx(sfx.audio, _overallSound * _sfxSound);
                else PlaySfxLoop(sfx.audio, _overallSound * _sfxSound);
            }
        }

        /// <summary>   
        /// Finds and plays a SfX sound with a given an id if such an Audio file exist.
        /// </summary>
        private void PlaySfx(int id, bool allowOverlay = true, bool loop = true)
        {
            AudioFile sfx = _sfxSounds.Find(e => id == e.id);
            if (sfx == null) return;
            if (!(!allowOverlay && _sfxMainSource.isPlaying))
            {
                if (!loop) PlaySfx(sfx.audio, _overallSound * _sfxSound);
                else PlaySfxLoop(sfx.audio, _overallSound * _sfxSound);
            }
        }

        /// <summary>
        /// Forces the SFX audio player to stop playing the current sound
        /// </summary>
        private void StopAmbient()
        {
            _ambientSource.Stop();
        }

        private void StopMusic()
        {
            _musicSource.Stop();
        }

        private void StopSfx()
        {
            _sfxMainSource.Stop();
        }

        public void PlayAudio(string name, AudioType audioType, bool loop = true, bool allowOverlay = true)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    PlayMusic(name);
                    break;
                case AudioType.Ambient:
                    PlayAmbient(name, allowOverlay, loop);
                    break;
                case AudioType.Sfx:
                    PlaySfx(name, allowOverlay, loop);
                    break;
                default:
                    Debug.LogWarning($"{audioType} is not a vaild Audio Type.");
                    break;
            }
        }

        public void PlayAudio(int id, AudioType audioType, bool loop = true, bool allowOverlay = true)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    PlayMusic(id);
                    break;
                case AudioType.Ambient:
                    PlayAmbient(id, allowOverlay, loop);
                    break;
                case AudioType.Sfx:
                    PlaySfx(id, allowOverlay, loop);
                    break;
                default:
                    Debug.LogWarning($"{audioType} is not a vaild Audio Type.");
                    break;
            }
        }

        public void PlayAudio(AudioClip clip, AudioType audioType, bool loop = true, bool allowOverlay = true)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    PlayMusic(clip, _overallSound * _musicSound);
                    break;
                case AudioType.Ambient:
                    if (!(!allowOverlay && _ambientSource.isPlaying))
                    {
                        if (!loop) PlayAmbient(clip, _overallSound * _ambientSound);
                        else PlayAmbientLoop(clip, _overallSound * _ambientSound);
                    }
                    break;
                case AudioType.Sfx:
                    if (!(!allowOverlay && _sfxMainSource.isPlaying))
                    {
                        if (!loop) PlaySfx(clip, _overallSound * _sfxSound);
                        else PlaySfxLoop(clip, _overallSound * _sfxSound);
                    }
                    break;
                default:
                    Debug.LogWarning($"{audioType} is not a vaild Audio Type.");
                    break;
            }
        }

        public void StopAudio(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    StopMusic();
                    break;
                case AudioType.Ambient:
                    StopAmbient();
                    break;
                case AudioType.Sfx:
                    StopSfx();
                    break;
                default:
                    Debug.LogWarning($"{audioType} is not a vaild Audio Type.");
                    break;
            }
        }

        public void SetOverallVolume(float volume)
        {
            _overallSound = volume;
            foreach (AudioSourceInfo info in _sfxSources) if (info != null && info.src != null) info.src.volume = info.vol * _overallSound * _sfxSound;
            foreach (AudioSourceInfo info in _musicSources) if (info != null && info.src != null) info.src.volume = info.vol * _overallSound * _musicSound;
            _ambientSource.volume = _overallSound * _ambientSound;
            _musicSource.volume = _overallSound * _musicSound;
        }

        public void SetSfXVolume(float volume)
        {
            _sfxSound = volume;
            foreach (AudioSourceInfo info in _sfxSources) if (info != null && info.src != null) info.src.volume = info.vol * _overallSound * _sfxSound;
            _ambientSource.volume = _overallSound * _ambientSound;
            _musicSource.volume = _overallSound * _musicSound;
        }

        public void SetAmbientVolume(float volume)
        {
            _ambientSound = volume;
            _ambientSource.volume = _overallSound * _ambientSound;
        }

        public void SetMusicVolume(float volume)
        {
            _musicSound = volume;
            foreach (AudioSourceInfo info in _musicSources) if (info != null && info.src != null) info.src.volume = info.vol * _overallSound * _musicSound;
            _musicSource.volume = _overallSound * _musicSound;
        }

        public float GetOverallVolume()
        {
            return _overallSound;
        }

        public float GetSfXVolume()
        {
            return _sfxSound;
        }

        public float GetMusicVolume()
        {
            return _musicSound;
        }

        public float GetAmbientVolume()
        {
            return _ambientSound;
        }

        public bool IsPlaying(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.Music:
                    return _musicSource.isPlaying;
                case AudioType.Ambient:
                    return _ambientSource.isPlaying;
                case AudioType.Sfx:
                    return _sfxMainSource.isPlaying;
                default:
                    return false;
            }
        }

        public bool SaveData<TData>(ref TData data) where TData : GameData
        {
            PlazmaDebug.Log("Saving data.", "Audio", Color.green, 2);

            if (data == null)
            {
                PlazmaDebug.LogWarning("Trying to save data but GameData is null.", "Audio", 1);
                return false;
            }

            if (!GameManager.HasMonoSystem<IDataPersistenceMonoSystem>())
            {
                PlazmaDebug.LogWarning($"Trying to save data but a DataPersistenceMonoSystem is not defined.", "Audio", 1);
                return false;
            }

            data.overallVolume = GetOverallVolume();
            data.musicVolume = GetMusicVolume();
            data.soundVolume = GetSfXVolume();
            data.ambentVolume = GetAmbientVolume();

            return true;

        }

        public bool LoadData<TData>(TData data) where TData : GameData
        {
            PlazmaDebug.Log("Loading data.", "Audio", Color.green, 2);

            if (data == null)
            {
                PlazmaDebug.LogWarning("Trying to laod data but GameData is null.", "Audio", 1);
                return false;
            }

            if (!GameManager.HasMonoSystem<IDataPersistenceMonoSystem>())
            {
                PlazmaDebug.LogWarning($"Trying to load data but a DataPersistenceMonoSystem is not defined.", "Audio", 1);
                return false;
            }

            SetOverallVolume(data.overallVolume);
            SetMusicVolume(data.musicVolume);
            SetSfXVolume(data.soundVolume);
            SetAmbientVolume(data.ambentVolume);

            return true;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            if (!_hasInitialized) return;
            Debug.Log("Yo");
            _sfxSources.Clear();
            _musicSources.Clear();

            AudioSource[] audioSrc = FindObjectsByType<AudioSource>();
            foreach (AudioSource audioSource in audioSrc)
            {
                AudioSourceInfo info = new AudioSourceInfo();
                info.src = audioSource;
                info.vol = audioSource.volume;

                if (audioSource.CompareTag("MusicSource"))
                {
                    if (!_musicSources.Contains(info))
                    {
                        _musicSources.Add(info);
                    }
                }
                else
                {
                    if (!_sfxSources.Contains(info))
                    {
                        _sfxSources.Add(info);
                    }
                }
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (!_hasInitialized) return;

            for (int i = _sfxSources.Count - 1; i >= 0; i--)
            {
                if (_sfxSources[i].src == null) _sfxSources.RemoveAt(i);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void Start()
        {
            PlazmaDebug.Log($"Setting game volume.", "Audio", Color.green, 2);

            _sfxSources = new List<AudioSourceInfo>();
            _musicSources = new List<AudioSourceInfo>();
            AudioSource[] audioSrc = FindObjectsByType<AudioSource>();
            foreach (AudioSource audioSource in audioSrc)
            {
                AudioSourceInfo info = new AudioSourceInfo();
                info.src = audioSource;
                info.vol = audioSource.volume;

                if (audioSource.CompareTag("MusicSource")) _musicSources.Add(info);
                else _sfxSources.Add(info);
            }

            SetOverallVolume(_overallSound);
            SetSfXVolume(_sfxSound);
            SetAmbientVolume(_ambientSound);
            SetMusicVolume(_musicSound);

            _hasInitialized = true;
        }
    }
}
