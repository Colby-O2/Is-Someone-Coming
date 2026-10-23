using PlazmaGames.Attribute;
using PlazmaGames.Audio;
using PlazmaGames.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColbyO.SJ26.UI
{
    using PlazmaGames.Math;
#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.UI;

    [CustomEditor(typeof(EventButton)), CanEditMultipleObjects]
    public class MenuButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
#endif

    public class EventButton : Button, IPointerDownHandler, IPointerUpHandler
    {
        public UnityEvent onPointerUp = new UnityEvent();
        public UnityEvent onPointerDown = new UnityEvent();
        public UnityEvent onPointerEnter = new UnityEvent();
        public UnityEvent onPointerExit = new UnityEvent();

        [SerializeField] private bool _playSound = true;
        [SerializeField, ReadOnly] private bool _isDisabled = false;

        [SerializeField] private bool _disabledAutoOverlayToggle = true;

        [SerializeField] private Image _icon;
        [SerializeField] private Image _overlay;

        [SerializeField] private Color _iconColor;
        [SerializeField] private Color _overlayColor;

        public bool DisableAutoIconToggle { get; set; }

        public bool IsDisabled
        {
            get
            {
                return _isDisabled;
            }
            set
            {
                _isDisabled = value;
                targetGraphic.color = _isDisabled ? colors.disabledColor : colors.normalColor;
                if (_overlay) _overlay.color = _isDisabled ? _overlayColor * colors.disabledColor : Color.white.SetA(0);
            }
        }

        public Color GetDisabledColor()
        {
            return colors.disabledColor;
        }

        public bool IsPointerUsed { get; set; }

        public Image GetOverlay()
        {
            return _overlay;
        }

        public Color GetOverlayColor()
        {
            return _overlayColor;
        }

        public void ToggleSound(bool state) => _playSound = state;

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (IsDisabled) return;

            IsPointerUsed = false;
            base.OnPointerUp(eventData);
            onPointerUp.Invoke();
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (IsDisabled) return;

            if (_playSound) GameManager.GetMonoSystem<IAudioMonoSystem>().PlayAudio("UIClick", PlazmaGames.Audio.AudioType.Sfx, false, true);
            IsPointerUsed = true;
            base.OnPointerDown(eventData);
            EventSystem.current.SetSelectedGameObject(null);
            onPointerDown.Invoke();
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (IsDisabled) return;
            onPointerEnter.Invoke();
            if (!DisableAutoIconToggle) ShowOverlay();
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (IsDisabled) return;
            onPointerExit.Invoke();
            if (!DisableAutoIconToggle) HideOverlay();
        }

        public void ShowOverlay()
        {
            if (_disabledAutoOverlayToggle) return;
            if (_icon) _icon.color = _iconColor;
            if (_overlay) _overlay.color = _overlayColor;
        }

        public void HideOverlay()
        {
            if (_disabledAutoOverlayToggle) return;
            if (_icon) _icon.color = Color.clear;
            if (_overlay) _overlay.color = Color.clear;
        }

        protected override void Awake()
        {
            base.Awake();
            HideOverlay();
        }
    }
}