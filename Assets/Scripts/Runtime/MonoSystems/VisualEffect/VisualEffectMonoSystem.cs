using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

using PlazmaGames.Core;

using ColbyO.VNTG.PSX;
using ColbyO.VNTG.CRT;

using ColbyO.Sj26.UI;

namespace ColbyO.Sj26.Effects
{
    public class VisualEffectMonoSystem : MonoBehaviour, IVisualEffectMonoSystem
    {
        private Volume _volume;

        private ScreenFade _screenFade;

        private void Start()
        {
            _volume = FindAnyObjectByType<Volume>();
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
            _volume = FindAnyObjectByType<Volume>();
            _screenFade = FindAnyObjectByType<ScreenFade>();
        }

        private void OnSceneUnload(Scene scene)
        {

        }

        public Promise FadeIn(float duration)
        {
            return _screenFade.FadeIn(duration);
        }

        public Promise FadeOut(float duration)
        {
            return _screenFade.FadeOut(duration);
        }

        public Promise WriteFadeOutText(string textToType, float typeSpeed, float duration)
        {
            return _screenFade.WriteFadeOutText(textToType, typeSpeed, duration);
        }

        public PSXEffectSettings GetPSXSettings()
        {
            if (_volume == null) _volume = FindAnyObjectByType<Volume>();

            if (_volume && _volume.profile && _volume.profile.TryGet(out PSXEffectSettings psx)) return psx;

            return null;
        }

        public CRTSettings GetCRTSettings()
        {
            if (_volume == null) _volume = FindAnyObjectByType<Volume>();

            if (_volume && _volume.profile && _volume.profile.TryGet(out CRTSettings crt)) return crt;

            return null;
        }
    }
}