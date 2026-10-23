using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

using PlazmaGames.Core;

using ColbyO.VNTG.PSX;
using ColbyO.VNTG.CRT;

using ColbyO.Sj26.UI;
using PlazmaGames.Core.Debugging;

namespace ColbyO.Sj26.Effects
{
    public class VisualEffectMonoSystem : MonoBehaviour, IVisualEffectMonoSystem
    {
        private Volume _volume;
        private ScreenFade _screenFade;

        private void Start()
        {
            EnsureVolume();
            _screenFade = FindAnyObjectByType<ScreenFade>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoad;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoad;
            SceneManager.sceneUnloaded -= OnSceneUnload;
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {
            _volume = null;
            EnsureVolume();
            _screenFade = FindAnyObjectByType<ScreenFade>();
        }

        private void OnSceneUnload(Scene scene)
        {
        }

        public Promise FadeIn(float duration) => _screenFade.FadeIn(duration);
        public Promise FadeOut(float duration) => _screenFade.FadeOut(duration);

        public Promise WriteFadeOutText(string textToType, float typeSpeed, float duration)
        {
            return _screenFade.WriteFadeOutText(textToType, typeSpeed, duration);
        }

        public PSXEffectSettings GetPSXSettings() => GetVolumeComponent<PSXEffectSettings>();
        public CRTSettings GetCRTSettings() => GetVolumeComponent<CRTSettings>();

        public void ResetPSXSettings() => ResetVolumeComponent<PSXEffectSettings>();
        public void ResetCRTSettings() => ResetVolumeComponent<CRTSettings>();

        public void ResetAllEffectSettings()
        {
            ResetPSXSettings();
            ResetCRTSettings();
        }

        private bool EnsureVolume()
        {
            if (_volume) return true;

            GameObject volumeGo = GameObject.FindWithTag("Volume");
            if (volumeGo) _volume = volumeGo.GetComponent<Volume>();
            if (!_volume) _volume = FindAnyObjectByType<Volume>();

            return _volume != null;
        }

        private T GetVolumeComponent<T>() where T : VolumeComponent
        {
            if (!EnsureVolume())
            {
                PlazmaDebug.LogError($"No Volume found in scene.", $"[{ typeof(T).Name}]" );
                return null;
            }

            VolumeProfile runtimeProfile = _volume.profile;
            if (!runtimeProfile)
            {
                PlazmaDebug.LogError($"Volume has no assigned VolumeProfile asset.", $"[{typeof(T).Name}]");
                return null;
            }

            if (runtimeProfile.TryGet(out T component))
            {
                return component;
            }

            PlazmaDebug.LogError($"Override is missing from the VolumeProfile.", $"[{typeof(T).Name}]");
            return null;
        }

        private void ResetVolumeComponent<T>() where T : VolumeComponent
        {
            if (!EnsureVolume() || !_volume.HasInstantiatedProfile() || !_volume.sharedProfile) return;

            if (_volume.sharedProfile.TryGet(out T defaultComp) && _volume.profile.TryGet(out T runtimeComp))
            {
                for (int i = 0; i < defaultComp.parameters.Count; i++)
                {
                    runtimeComp.parameters[i].overrideState = defaultComp.parameters[i].overrideState;
                    runtimeComp.parameters[i].SetValue(defaultComp.parameters[i]);
                }
            }
        }
    }
}