using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlazmaGames.UI;
using PlazmaGames.Console;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using PlazmaGames.SO.Databases;
using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

namespace PlazmaGames.UI.Views
{
    [RequireComponent(typeof(PlayerInput))]
    public class DeveloperConsoleView : View
    {
        [Header("Console Settings")]
        [SerializeField] private DeveloperConsoleDatabase _db;
        [SerializeField] private Color _defaultColor = Color.white;

        [Header("References")]
        [SerializeField] private PlayerInput _playerInput;
        [SerializeField] private TMP_InputField _commandInput;
        [SerializeField] private TMP_Text _textAera;
        [SerializeField] private Button _submitButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private ScrollRect _scrollRect;

        private int _historySlot = 0;
        
        private List<string> _commandHistory;
        private DeveloperConsole _console;

        private InputAction _nextCommandAction;
        private InputAction _previousCommandAction;
        private InputAction _defaultCommand;

        /// <summary>
        /// Scroll console view to bottom at the end of the frame
        /// </summary>
        private IEnumerator ScrollToBottom()
        {
            // we need to wait for the current frame to end and all canvas activity
            // to finish before we scroll the view
            yield return new WaitForEndOfFrame();
            _scrollRect.verticalNormalizedPosition = 0;
        }

        /// <summary>
        /// Moves input cursor to the end at the end of the frame
        /// </summary>
        private IEnumerator InputCursorToEnd()
        {
            yield return new WaitForEndOfFrame();
            _commandInput.MoveTextEnd(false);
        }

        /// <summary>
        /// Prints a command to the Console Window.
        /// </summary>
        private void PrintToCommandWindow(string msg, Color? color = null)
        {
            _textAera.text += $" <color=#{ColorUtility.ToHtmlStringRGBA(color ?? _defaultColor)}>{msg}</color>\n";
            if (gameObject.activeSelf) StartCoroutine(ScrollToBottom());
        }

        /// <summary>
        /// Display's a help message to the console
        /// </summary>
        private void Help()
        {
            string res = string.Empty;

            res += "<align=center><b>Command List</b></align>\n";

            foreach (ConsoleCommand command in _db.GetAllEntries())
            {
                res += "<align=left>" + command.Command + " - " + command.Description + "</align>\n";
            }

            ConsoleResponse msg = new(res, ResponseType.Help);

            PrintToCommandWindow(msg.Message, msg.MessageColor);
        }

        /// <summary>
        /// Resets the input field in the console
        /// </summary>
        private void ResetInputField()
        {
            _commandInput.text = "";
            _historySlot = 0;
            _commandInput.ActivateInputField();
        }

        /// <summary>
        /// Clears all command history
        /// </summary>
        private void ClearCommandHistory()
        {
            _commandHistory = new();
            _historySlot = 0;
        }

        /// <summary>
        /// Process an inputed command.
        /// </summary>
        private void ProcessCommand(string val)
        {
            PrintToCommandWindow(val);
            ConsoleResponse msg = _console.ProcessCommand(val);
            if (_commandHistory.Count == 0 || !_commandHistory[^1].Equals(val, System.StringComparison.OrdinalIgnoreCase))
            {
                _commandHistory.Add(val);
            }

            if (msg.Type == ResponseType.Help) Help();
            else if (msg.Type == ResponseType.Clear) _textAera.text = string.Empty;
            else if (msg.Type == ResponseType.ClearHistory) ClearCommandHistory();
            else if (msg.Type != ResponseType.None && msg != null) PrintToCommandWindow(msg.Message, msg.MessageColor);
            ResetInputField();
        }

        /// <summary>
        /// Callback for the Submit button on the developer console.
        /// </summary>
        private void Submit() => ProcessCommand(_commandInput.text);

        /// <summary>
        /// Puts the next commands in the history in the input box
        /// </summary>
        private void NextCommand(InputAction.CallbackContext e)
        {
            if (_historySlot < _commandHistory.Count) _historySlot++;
            if (_historySlot > _commandHistory.Count || _historySlot < 1) return;
            _commandInput.text = _commandHistory[^_historySlot];
            if (gameObject.activeSelf) StartCoroutine(InputCursorToEnd());
        }

        /// <summary>
        /// Puts the previous commands in the history in the input box
        /// </summary>
        private void PreviousCommand(InputAction.CallbackContext e)
        {
            if (_historySlot > 0) _historySlot--;
            if (_historySlot < 1 || _historySlot > _commandHistory.Count)
            {
                _commandInput.text = "";
                return;
            }
            _commandInput.text = _commandHistory[^_historySlot];
            if (gameObject.activeSelf) StartCoroutine(InputCursorToEnd());
        }

        private void BackToDefaultCommand(InputAction.CallbackContext e) => ResetInputField();

        private void Close()
        {
            GameManager.GetMonoSystem<IUIMonoSystem>().ShowLast();
        }

        private void AddListeners()
        {
            _commandInput.onSubmit.AddListener(ProcessCommand);
            _submitButton.onClick.AddListener(Submit);
            _closeButton.onClick.AddListener(Close);
        }

        private void RemoveListeners()
        {
            _commandInput.onSubmit.RemoveListener(ProcessCommand);
            _submitButton.onClick.RemoveListener(Submit);
            _closeButton.onClick.RemoveListener(Close);
        }

        private void OnDebugReceieved(string log, DebugType type, Color? color, int verboseLevel)
        {
            Color col = color ?? type.GetColor();

            if (PlazmaDebug.CanLog(verboseLevel)) PrintToCommandWindow($"<color=#{ColorUtility.ToHtmlStringRGBA(col)}>{type.GetPrefix()}</color>{log}");
        }

        public override void Init()
        {
            _db.InitDatabase();

            PlazmaDebug.OnDebug.AddListener(OnDebugReceieved);

            ResetInputField();

            _textAera.text = string.Empty;

            _console = new DeveloperConsole(_db.GetAllEntries());

            if (_playerInput == null) _playerInput = GetComponent<PlayerInput>();

            ClearCommandHistory();

            _nextCommandAction = _playerInput.actions["HistoryNext"];
            _previousCommandAction = _playerInput.actions["HistoryPrevious"];
            _defaultCommand = _playerInput.actions["DefaultCommand"];
        }

        public override void Show()
        {
            base.Show();

            ResetInputField();

            AddListeners();

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;

            if (_nextCommandAction != null) _nextCommandAction.performed += NextCommand;
            if (_previousCommandAction != null) _previousCommandAction.performed += PreviousCommand;
            if (_defaultCommand != null) _defaultCommand.performed += BackToDefaultCommand;

            StartCoroutine(ScrollToBottom());
            StartCoroutine(InputCursorToEnd());
        }

        public override void Hide()
        {
            base.Hide();

            RemoveListeners();

            if (_nextCommandAction != null) _nextCommandAction.performed -= NextCommand;
            if (_previousCommandAction != null) _previousCommandAction.performed -= PreviousCommand;
            if (_defaultCommand != null) _defaultCommand.performed -= BackToDefaultCommand;
        }

        private void OnDestroy()
        {
            PlazmaDebug.OnDebug.RemoveListener(OnDebugReceieved);
            RemoveListeners();
        }
    }
}
