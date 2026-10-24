using ColbyO.SJ26.Enviroment;
using ColbyO.SJ26.UI;
using InteractionSystem.Controls;
using InteractionSystem.UI;
using PlazmaGames.Core;
using PlazmaGames.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ColbyO.SJ26
{
    public class EndView : View
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private GameObject _view;
        [SerializeField] private AudioSource _musicSource;

        [SerializeField] private GameObject _mainMenuCamera;
        [SerializeField] private GameObject _playerCamera;

        [SerializeField] private EventButton _quit;

        private void Update()
        {
            HandleCursor();
        }

        public override void Init()
        {
            _quit.onClick.AddListener(Quit);
        }

        public override void Show()
        {
            base.Show();

            _view.SetActive(true);

            VirtualCaster.ShowCursor();
            SJ26GameManager.HideCursor();

            _mainMenuCamera.SetActive(true);
            _playerCamera.SetActive(false);

            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed += OnClick;

            _musicSource.volume = GameManager.GetMonoSystem<IUIMonoSystem>().GetView<MainMenuView>().GetMusicVolume();
            if (!_musicSource.isPlaying) _musicSource.Play();

            SJ26GameManager.LockMovement = true;
        }

        public override void Hide()
        {
            base.Hide();
            //VirtualCaster.HideCursor();
            SJ26GameManager.PlayerInteractor.Controls.InspectionClickAction.performed -= OnClick;
        }

        public void Quit()
        {
            Application.Quit();
        }

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
    }
}
