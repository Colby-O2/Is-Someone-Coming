using ColbyO.Sj26.Effects;
using ColbyO.SJ26.Enviroment;
using ColbyO.SJ26.GameLogic;
using InteractionSystem.Controls;
using InteractionSystem.UI;
using PlazmaGames.Audio;
using PlazmaGames.Core;
using PlazmaGames.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ColbyO.SJ26.UI
{
    public class MainMenuView : View
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GameObject _view;
        [SerializeField] private AudioSource _musicSource;

        [SerializeField] private EventButton _play;
        [SerializeField] private EventButton _settings;
        [SerializeField] private EventButton _quit;

        [SerializeField] private GameObject _mainMenuCamera;
        [SerializeField] private GameObject _playerCamera;

        private Coroutine _musicFadeRoutine;
        private float _startVol;

        private void Awake()
        {
            _startVol = _musicSource.volume;
        }

        private void Update()
        {
            HandleCursor();
        }

        private void Start()
        {
            GameManager.GetMonoSystem<ILightMonoSystem>().Mute(true);
        }

        public override void Init()
        {
            _play.onPointerDown.AddListener(Play);
            _settings.onPointerDown.AddListener(Settings);
            _quit.onPointerDown.AddListener(Quit);
        }

        public override void Show()
        {
            base.Show();

            _view.SetActive(true);

            VirtualCaster.ShowCursor();
            SJ26GameManager.HideCursor();

            SJ26GameManager.HasStarted = false;

            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed += OnClick;

            SJ26GameManager.LockMovement = true;
        }

        public override void Hide()
        {
            base.Hide();
            //VirtualCaster.HideCursor();
            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed -= OnClick;
        }

        public float GetMusicVolume() => _musicSource.volume; 

        public void OnClick(InputAction.CallbackContext ctx)
        {
            ClickUI();
        }

        private void HandleCursor()
        {
            float sensitivity = InputDeviceHandler.IsCurrentGamepad
                ? SJ26GameManager.PlayerInteractor.Controls.ControllerMouseSensitivity
                : SJ26GameManager.PlayerInteractor.Controls.KeybaordMouseSensitivity;

            Vector2 delta = SJ26GameManager.PlayerInteractor.Controls.InspectionCursorAction.ReadValue<Vector2>() * sensitivity;

            VirtualCaster.WrapCursorPosition(delta);
        }

        public void ClickUI()
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = VirtualCaster.GetVirtualMousePosition(),
                button = PointerEventData.InputButton.Left
            };

            List<RaycastResult> results = new List<RaycastResult>();
            _canvas.GetComponent<GraphicRaycaster>().Raycast(eventData, results);

            if (results.Count == 0) return;

            GameObject go = results[0].gameObject;
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerClickHandler);
        }

        private IEnumerator FadeOutMusic(AudioSource source, float duration)
        {
            float startVolume = _startVol;

            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume * GameManager.GetMonoSystem<IAudioMonoSystem>().GetOverallVolume(), 0f, time / duration);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
        }

        private void Play()
        {
            SJ26GameManager.HideCursor();
            VirtualCaster.HideCursor();

            float fadeOutTime = 3f;

            if (_musicFadeRoutine != null) StopCoroutine(_musicFadeRoutine);
            _musicFadeRoutine = StartCoroutine(FadeOutMusic(_musicSource, fadeOutTime));

            GameManager.GetMonoSystem<IVisualEffectMonoSystem>().FadeOut(fadeOutTime)
            .Then(_ =>
            {
                SJ26GameManager.HasStarted = true;

                _view.SetActive(false);
                _mainMenuCamera.SetActive(false);
                _playerCamera.SetActive(true);

                GameManager.GetMonoSystem<ILightMonoSystem>().Mute(false);
                GameManager.GetMonoSystem<IUIMonoSystem>().Show<GameView>(remeber: false);
                GameManager.GetMonoSystem<IGameLogicMonoSystem>().TriggerEvent("Act1");
            });
        }

        private void Settings()
        {
            GameManager.GetMonoSystem<IUIMonoSystem>().Show<SettingsView>();
        }

        private void Quit()
        {
            Application.Quit();
        }
    }
}