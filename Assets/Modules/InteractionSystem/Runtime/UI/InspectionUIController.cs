using InteractionSystem.Attribute;
using InteractionSystem.Helpers;
using InteractionSystem.UI;
using UnityEngine;
using UnityEngine.Events;

namespace InteractionSystem
{
    public sealed class InspectionUIController : MonoBehaviour
    {
        [SerializeField] private bool _useDefaultToggleBehaviour = true;

        [SerializeField] private UIIcon _escapeButton;
        [SerializeField] private UIIcon _rotateButton;
        [SerializeField] private UIIcon _zoomButton;
        [SerializeField] private UIIcon _interactButton;
        [SerializeField] private UIIcon _readButton;

        [SerializeField] private UIIcon _nextButton;
        [SerializeField] private UIIcon _backButton;

        [SerializeField] private GameObject _readPanel;
        [SerializeField] private TMPro.TMP_InputField _readText;

        [SerializeField] private GameObject _titlePanel;
        [SerializeField] private TMPro.TMP_Text _titleText;

        public UnityEvent OnShow = new UnityEvent();
        public UnityEvent OnHide = new UnityEvent();

        public void ToggleReadPanel() => _readPanel.SetActive(!_readPanel.activeSelf);
        public void SetReadText(string text) => _readText.text = text;
        public void SetTitleText(string text) => _titleText.text = text;

        public void SetRotateButton(bool state) => _rotateButton.SetActive(state);
        public void SetZoomButton(bool state) => _zoomButton.SetActive(state);
        public void SetReadButton(bool state) => _readButton.SetActive(state);
        public void SetInteractButton(bool state) => _interactButton.SetActive(state);

        public void SetNextButton(bool state) => _nextButton.SetActive(state);
        public void SetBackButton(bool state) => _backButton.SetActive(state);

        public void SetReadPanel(bool state)
        {
            if (_readPanel.activeSelf != state) _readPanel.SetActive(state);
        }

        public void SetTitle(bool state)
        {
            if (_titlePanel.activeSelf != state) _titlePanel.SetActive(state);
        }

        private void Show()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            VirtualCaster.ShowCursor();

        }

        private void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            VirtualCaster.HideCursor();
        }

        private void Awake()
        {
            if (_useDefaultToggleBehaviour)
            {
                OnShow.AddListener(Show);
                OnHide.AddListener(Hide);
                Hide();
            }
        }

        private void Update()
        {
            if (_escapeButton.IsActive()) _escapeButton.UpdateIconMaterial();
            if (_rotateButton.IsActive()) _rotateButton.UpdateIconMaterial();
            if (_zoomButton.IsActive()) _zoomButton.UpdateIconMaterial();
            if (_interactButton.IsActive()) _interactButton.UpdateIconMaterial();
            if (_readButton.IsActive()) _readButton.UpdateIconMaterial();
            if (_nextButton.IsActive()) _nextButton.UpdateIconMaterial();
            if (_backButton.IsActive()) _backButton.UpdateIconMaterial();
        }
    }
}
