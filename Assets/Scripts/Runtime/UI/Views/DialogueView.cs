using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

using System.Collections;
using System.Collections.Generic;

using TMPro;

using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.UI;

using DialogueGraph.Data;

using InteractionSystem.UI;

using ColbyO.SJ26.Dialogue;

namespace ColbyO.SJ26.UI
{
    public class DialogueView : View
    {
        [Header("Dialogue UI")]
        [SerializeField] private GameObject _dialogueHolder;
        [SerializeField] private TMP_Text _dialogueAvatarName;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private GameObject _dialogueHint;
        [SerializeField] private UIIcon _hintIcon;

        [Header("Settings")]
        [SerializeField] private float _typeSpeed = 0.05f;
        [SerializeField] private float _delayBetweenDialogues = 3f;
        [SerializeField] private int _characterThreshold = 50;
        [SerializeField] private float _extraTimePerCharacter = 0.05f;

        [Header("Choice UI")]
        [SerializeField] private GameObject _choiceHolder;
        [SerializeField] private List<UIIcon> _choiceIcons;
        [SerializeField] private List<TMP_Text> _choiceTexts;
        [SerializeField] private List<Image> _choiceBackgrounds;

        [Header("Input")]
        [SerializeField] private InputAction _nextAction;
        [SerializeField] private List<InputAction> _choiceActions;

        [Header("Audio")]
        [SerializeField] private AudioSource _as;

        private Coroutine _typeRoutine;

        [Header("Debugging")]
        [SerializeField, ReadOnly] private bool _isTyping;
        [SerializeField, ReadOnly] private bool _isWaitingForInput;
        [SerializeField, ReadOnly] private bool _isPassive;
        [SerializeField, ReadOnly] string _fullCurrentMessage;
        [SerializeField, ReadOnly] private float _timeWaitingForInput;
        [SerializeField, ReadOnly] private float _currentDialogueDelay;
        [SerializeField, ReadOnly] private bool _canAutoAdvance;

        public UnityEvent<int> OnChoiceSelected { get; private set; }
        public UnityEvent OnRequestNext { get; private set; }

        private void Awake()
        {
            OnChoiceSelected = new UnityEvent<int>();
            OnRequestNext = new UnityEvent();

            OnChoiceSelected.AddListener(GameManager.GetMonoSystem<IDialogueMonoSystem>().SelectedChoice);

            OnRequestNext.AddListener(OnDiagoueEnd);
        }

        private void OnDestroy()
        {
            if (_typeRoutine != null)
            {
                StopCoroutine(_typeRoutine);
                _typeRoutine = null;
            }
        }

        private void Update()
        {
            UpdateIcons();

            if (SJ26GameManager.IsPaused) return;

            if (_isWaitingForInput && _isPassive && _canAutoAdvance)
            {
                _timeWaitingForInput += Time.deltaTime;
                if (_timeWaitingForInput > _currentDialogueDelay)
                {
                    _timeWaitingForInput = 0.0f;
                    OnRequestNext?.Invoke();
                }
            }
        }

        public override void Init()
        {
            HideDialogue();
            HideChoice();
            RegisterInputs();

            _dialogueHint.SetActive(false);
        }

        public override void Show()
        {
            base.Show();
        }

        public override void Hide()
        {
            DisableChoiceInput();
            HideChoice();
            HideDialogue();
        }

        private void RegisterInputs()
        {
            _nextAction.performed += _ => HandleNextPressed();

            for (int i = 0; i < _choiceActions.Count; i++)
            {
                int index = i;
                _choiceActions[i].performed += _ => OnChooseChoice(index);
            }
        }

        private void UpdateIcons()
        {
            foreach (UIIcon icon in _choiceIcons)
            {
                if (icon.IsActive()) icon.UpdateIconMaterial();
            }

            if (_hintIcon.IsActive()) _hintIcon.UpdateIconMaterial();
        }

        public void DisplayMessage(string actor, string message, bool passive = false)
        {
            HideChoice();

            _nextAction.Enable();

            _canAutoAdvance = false;

            _isPassive = passive;
            _fullCurrentMessage = GameManager.GetMonoSystem<IDialogueMonoSystem>().ParaseMsgForVariables(message);
            _dialogueAvatarName.text = GameManager.GetMonoSystem<IDialogueMonoSystem>().ParaseMsgForVariables(actor);

            _currentDialogueDelay = CalculateDialogueDelay(_fullCurrentMessage);
            _timeWaitingForInput = 0.0f;

            _dialogueHolder.SetActive(true);
            _dialogueHint.SetActive(false);

            if (_typeRoutine != null)
            {
                if (_as && _as.isPlaying) _as.Stop();
                StopCoroutine(_typeRoutine);
            }

            // Unity 6.6 Sucks. 
            if (this == null) return;

            _typeRoutine = StartCoroutine(TypewriterRoutine(_fullCurrentMessage));
        }

        private float CalculateDialogueDelay(string message)
        {
            if (string.IsNullOrEmpty(message)) return _delayBetweenDialogues;

            int visibleCount = 0;
            bool insideTag = false;

            for (int i = 0; i < message.Length; i++)
            {
                if (message[i] == '<') insideTag = true;
                else if (message[i] == '>') insideTag = false;
                else if (!insideTag) visibleCount++;
            }

            int extraChars = Mathf.Max(0, visibleCount - _characterThreshold);
            return _delayBetweenDialogues + (extraChars * _extraTimePerCharacter);
        }

        public void DisplayChoices(List<DialogueChoiceData> choices)
        {
            HideDialogue();

            _isWaitingForInput = false;
            _dialogueHint.SetActive(false);
            _choiceHolder.SetActive(true);

            for (int i = 0; i < _choiceTexts.Count; i++)
            {
                bool isActive = i < choices.Count;
                _choiceTexts[i].gameObject.SetActive(isActive);
                if (isActive)
                {
                    _choiceTexts[i].text = GameManager.GetMonoSystem<IDialogueMonoSystem>().ParaseMsgForVariables(choices[i].Text);
                    _choiceIcons[i].SetActive(true);
                    _choiceActions[i].Enable();
                }
                else
                {
                    _choiceTexts[i].text = string.Empty;
                    _choiceIcons[i].SetActive(false);
                    if (_choiceBackgrounds[i].gameObject.activeSelf) _choiceBackgrounds[i].gameObject.SetActive(false);
                    _choiceActions[i].Disable();
                }
            }
        }

        public void HideDialogue()
        {
            if (_typeRoutine != null)
            {
                StopCoroutine(_typeRoutine);
                _typeRoutine = null;
            }

            if (_as && _as.isPlaying) _as.Stop();

            _isTyping = false;
            _isWaitingForInput = false;

            _dialogueHolder.SetActive(false);
        }

        public void HideChoice()
        {
            _choiceHolder.SetActive(false);
            DisableChoiceInput();
        }

        private IEnumerator TypewriterRoutine(string text)
        {
            _isTyping = true;
            _dialogueText.text = "";
            int visibleCharacters = 0;

            if (_as) _as.Play();

            while (visibleCharacters < text.Length)
            {
                while (SJ26GameManager.IsPaused)
                {
                    if (_as) _as.Pause();
                    yield return null;
                    continue;
                }

                if (_as && !_as.isPlaying) _as.UnPause();

                if (text[visibleCharacters] == '<')
                {
                    int endTag = text.IndexOf('>', visibleCharacters);
                    if (endTag != -1) visibleCharacters = endTag + 1;
                }
                else
                {
                    visibleCharacters++;
                }

                _dialogueText.text = text.Substring(0, visibleCharacters);
                yield return new WaitForSeconds(_typeSpeed);
            }

            if (_as) _as.Stop();

            if (!_dialogueHint.activeSelf && !_isPassive) _dialogueHint.SetActive(true);

            CompleteTyping();
        }

        private void OnDiagoueEnd()
        {
            HideDialogue();
            _canAutoAdvance = false;
            _isWaitingForInput = false;
            _timeWaitingForInput = 0.0f;
            OnChoiceSelected?.Invoke(0);
        }

        private void CompleteTyping()
        {
            if (_typeRoutine != null) StopCoroutine(_typeRoutine);
            if (_as && _as.isPlaying) _as.Stop();
            _dialogueText.text = _fullCurrentMessage;
            _isTyping = false;
            _isWaitingForInput = true;
            _timeWaitingForInput = 0.0f;

            _canAutoAdvance = true;
        }

        private void HandleNextPressed()
        {
            if (SJ26GameManager.IsPaused || _isPassive) return;

            if (_isTyping)
            {
                if (_as && _as.isPlaying) _as.Stop();
                if (!_dialogueHint.activeSelf && !_isPassive) _dialogueHint.SetActive(true);
                CompleteTyping();
            }
            else if (_isWaitingForInput)
            {
                _isWaitingForInput = false;
                OnRequestNext?.Invoke();
            }
        }

        private void OnChooseChoice(int index)
        {
            if (!_choiceHolder.activeSelf || index >= _choiceTexts.Count) return;

            HideChoice();
            OnChoiceSelected?.Invoke(index);
        }

        private void EnableChoiceInput()
        {
            foreach (InputAction action in _choiceActions) action.Enable();
        }

        private void DisableChoiceInput()
        {
            foreach (InputAction action in _choiceActions) action.Disable();
        }
    }
}