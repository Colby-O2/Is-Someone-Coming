using ColbyO.SJ26.UI;
using InteractionSystem.UI;
using PlazmaGames.Core;
using PlazmaGames.UI;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColbyO.SJ26
{
    public class GameView : View
    {
        [Header("Pause Settings")]
        [SerializeField] private InputAction _pauseAction;
        [SerializeField] private UIIcon _cameraHintIcon;

        [Header("Task Settings")]
        [SerializeField] private GameObject _taskHolder;
        [SerializeField] private TMP_Text _taskText;
        [SerializeField] float _typeSpeed = 0.1f;
        private Coroutine _taskTypingRoutine;

        [SerializeField] private AudioSource _as;

        public InputAction Action { get => _pauseAction; }

        private IEnumerator WriteText(string textToType, float typeSpeed)
        {
            _taskText.text = string.Empty;
            if (string.IsNullOrEmpty(textToType))
                yield break;

            WaitForSeconds wait = new WaitForSeconds(typeSpeed);

            if (_as != null && !_as.isPlaying) _as.Play();

            for (int i = 0; i < textToType.Length; i++)
            {
                while (SJ26GameManager.IsPaused)
                {
                    if (_as != null && _as.isPlaying) _as.Stop();
                    yield return null;
                }

                if (_as != null && !_as.isPlaying) _as.Play();

                _taskText.text += textToType[i];
                yield return wait;
            }

            if (_as != null && _as.isPlaying) _as.Stop();
        }

        public override void Init()
        {
            _pauseAction.performed -= HandlePause;
            _pauseAction.performed += HandlePause;
            _pauseAction.Enable();
        }

        public override void Hide()
        {
            
        }

        public void ShowTask(string taskDesp)
        {
            _taskHolder.SetActive(true);
            _taskText.text = taskDesp;

            if (_taskTypingRoutine != null) StopCoroutine(_taskTypingRoutine);
            _taskText.text = string.Empty;

            if (this == null) return;

            _taskTypingRoutine = StartCoroutine(WriteText(taskDesp, _typeSpeed));
        }

        public void UpdateTask(string taskDesp)
        {
            if (_taskTypingRoutine != null) StopCoroutine(_taskTypingRoutine);
            _taskText.text = taskDesp;
        }
        
        public void HideTask()
        {
            if (_taskTypingRoutine != null) StopCoroutine(_taskTypingRoutine);
            _taskHolder.SetActive(false);
            _taskText.text = string.Empty;
        }

        private void HandlePause(InputAction.CallbackContext e)
        {
            if (!SJ26GameManager.HasStarted || SJ26GameManager.PlayerInteractor.IsInspecting()) return;

            if (SJ26GameManager.IsPaused)
            {
                if (GameManager.GetMonoSystem<IUIMonoSystem>().GetCurrentViewIs<PauseMenuView>()) GameManager.GetMonoSystem<IUIMonoSystem>().GetView<PauseMenuView>().Resume();
                else GameManager.GetMonoSystem<IUIMonoSystem>().ShowLast();
            }
            else
            {
                GameManager.GetMonoSystem<IUIMonoSystem>().Show<PauseMenuView>();
            }
        }
    }
}