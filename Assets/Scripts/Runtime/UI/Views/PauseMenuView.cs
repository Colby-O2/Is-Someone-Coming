using InteractionSystem.Controls;
using InteractionSystem.UI;
using PlazmaGames.Core;
using PlazmaGames.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ColbyO.SJ26.UI
{
    public class PauseMenuView : View
    {
        [SerializeField] private Canvas _canvas;

        [SerializeField] private GameObject _backdrop;

        [SerializeField] private EventButton _resume;
        [SerializeField] private EventButton _settings;
        [SerializeField] private EventButton _quit;

        private void Update()
        {
            HandleCursor();
        }

        public override void Init()
        {
            _resume.onPointerDown.AddListener(Resume);
            _settings.onPointerDown.AddListener(Settings);
            _quit.onPointerDown.AddListener(Quit);
        }

        public override void Show()
        {
            base.Show();
            _backdrop.SetActive(true);
            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed += OnClick;
            SJ26GameManager.IsPaused = true;
            VirtualCaster.ShowCursor();
        }

        public override void Hide()
        {
            base.Hide();
            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed -= OnClick;
        }

        public void OnClick(InputAction.CallbackContext ctx)
        {
            ClickSJ26();
        }

        private void HandleCursor()
        {
            float sensitivity = InputDeviceHandler.IsCurrentGamepad
                ? SJ26GameManager.PlayerInteractor.Controls.ControllerMouseSensitivity
                : SJ26GameManager.PlayerInteractor.Controls.KeybaordMouseSensitivity;

            Vector2 delta = SJ26GameManager.PlayerInteractor.Controls.InspectionCursorAction.ReadValue<Vector2>() * sensitivity;

            VirtualCaster.WrapCursorPosition(delta);
        }

        public void ClickSJ26()
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

        public void Resume()
        {
            VirtualCaster.HideCursor();
            SJ26GameManager.IsPaused = false;
            _backdrop.SetActive(false);
            GameManager.GetMonoSystem<IUIMonoSystem>().Show<GameView>();
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