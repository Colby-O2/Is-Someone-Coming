using InteractionSystem.Controls;
using InteractionSystem.Helpers;
using InteractionSystem.UI;
using PlazmaGames.Audio;
using PlazmaGames.Core;
using PlazmaGames.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ColbyO.SJ26.UI
{
    public class SettingsView : View
    {
        [SerializeField] private Canvas _canvas;

        [SerializeField] private MovementSettings _playerSettings;

        [SerializeField] private int _numberOfSteps = 10;

        [SerializeField] private Image _volume;
        [SerializeField] private EventButton _volumeUp;
        [SerializeField] private EventButton _volumeDown;

        [SerializeField] private Image _sensitivity;
        [SerializeField] private EventButton _sensitivityUp;
        [SerializeField] private EventButton _sensitivityDown;

        [SerializeField] private Toggle _invertY;

        [SerializeField] private EventButton _back;

        private int _volumeStep;
        private int _sensitivityStep;

        private void Update()
        {
            HandleCursor();
        }

        public override void Init()
        {
            _back.onPointerDown.AddListener(Back);

            _volumeUp.onPointerDown.AddListener(VolumeUp);
            _volumeDown.onPointerDown.AddListener(VolumeDown);

            _sensitivityUp.onPointerDown.AddListener(SensitivityUp);
            _sensitivityDown.onPointerDown.AddListener(SensitivityDown);

            _invertY.onValueChanged.AddListener(ToggleInvertY);

            _invertY.isOn = _playerSettings.InvertLookY;

            float currentVolume = GameManager.GetMonoSystem<IAudioMonoSystem>().GetOverallVolume();
            _volumeStep = Mathf.RoundToInt(currentVolume * _numberOfSteps);

            _sensitivityStep = Mathf.RoundToInt(_playerSettings.Sensitivity * _numberOfSteps);

            UpdateUI();
        }

        public override void Show()
        {
            base.Show();
            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed += OnClick;
            VirtualCaster.ShowCursor();
        }

        public override void Hide()
        {
            base.Hide();
            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed -= OnClick;
            //VirtualCaster.HideCursor();
        }

        private void Back()
        {
            GameManager.GetMonoSystem<IUIMonoSystem>().ShowLast();
        }

        private float GetSensitivityAdjustedValue(float input, float exp = 2f)
        {
            return Mathf.Pow(input, exp);
        }

        private void ToggleInvertY(bool value)
        {
            _playerSettings.InvertLookY = value;
        }

        private void VolumeUp()
        {
            _volumeStep = Mathf.Clamp(_volumeStep + 1, 0, _numberOfSteps);
            ApplyVolume();
        }

        private void VolumeDown()
        {
            _volumeStep = Mathf.Clamp(_volumeStep - 1, 0, _numberOfSteps);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            float value = (float)_volumeStep / _numberOfSteps;
            GameManager.GetMonoSystem<IAudioMonoSystem>().SetOverallVolume(value);
            UpdateUI();
        }

        private void SensitivityUp()
        {
            _sensitivityStep = Mathf.Clamp(_sensitivityStep + 1, 0, _numberOfSteps);
            ApplySensitivity();
        }

        private void SensitivityDown()
        {
            _sensitivityStep = Mathf.Clamp(_sensitivityStep - 1, 0, _numberOfSteps);
            ApplySensitivity();
        }

        private void ApplySensitivity()
        {
            float normalized = (float)_sensitivityStep / _numberOfSteps;

            float adjusted = GetSensitivityAdjustedValue(normalized);

            _playerSettings.Sensitivity = adjusted;
            UpdateUI();
        }

        public void OnClick(InputAction.CallbackContext ctx)
        {
            ClickUI();
        }

        private void UpdateUI()
        {
            _volume.transform.localScale = _volume.transform.localScale.SetX((float)_volumeStep / _numberOfSteps);
            _sensitivity.transform.localScale = _sensitivity.transform.localScale.SetX((float)_sensitivityStep / _numberOfSteps);
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

            Toggle toggle = go.GetComponentInParent<Toggle>();
            if (toggle != null)
            {
                toggle.isOn = !toggle.isOn;
                return;
            }

            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerEnterHandler);
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, eventData, ExecuteEvents.pointerClickHandler);
        }
    }
}