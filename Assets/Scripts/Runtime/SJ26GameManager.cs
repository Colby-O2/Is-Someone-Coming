using ColbyO.SJ26.Dialogue;
using DialogueGraph.Data;
using InteractionSystem;
using PlazmaGames.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ColbyO.SJ26
{
    public class SJ26GameManager : GameManager
    {
        [SerializeField] private GameObject _monoSystemHolder;

        [SerializeField] private Preferences _preferences;
        
        public static Preferences Preferences { get => (Instance as SJ26GameManager)._preferences; }

        public static Scheduler GlobalScheduler = new Scheduler();

        public static bool IsPaused { get; set; }
        public static bool LockMovement { get; set; }
        public static bool HasStarted { get; set; }

        public static MovementController Player;
        public static SitController PlayerSit;
        public static AnimationController PlayerAnimator;
        public static InteractorController PlayerInteractor;

        private void Awake()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = 1000;
        }

        private void Start()
        {
            HideCursor();

            Player = GameObject.FindWithTag("Player").GetComponent<MovementController>();
            PlayerSit = Player.GetComponent<SitController>();
            PlayerAnimator = Player.GetComponent<AnimationController>();
            PlayerInteractor = Player.GetComponent<InteractorController>();
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

        private void Update()
        {
            GlobalScheduler.Tick(Time.deltaTime);
            if (Keyboard.current.jKey.wasPressedThisFrame) GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise("Test");
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                UseCustomCursor();
            }
        }

        public static void UseCustomCursor()
        {
            if (Instance)
            {
                Cursor.SetCursor(Preferences.Cursor, Vector2.zero, CursorMode.Auto);
            }
        }

        public static void HideCursor()
        {
            UseCustomCursor();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void ShowCursor()
        {
            UseCustomCursor();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        public override string GetApplicationName()
        {
            return nameof(SJ26GameManager);
        }

        public override string GetApplicationVersion()
        {
            return "v0.0.1";
        }

        protected override void OnInitalized()
        {
            _monoSystemHolder.SetActive(true);
        }

        private void OnSceneLoad(Scene scene, LoadSceneMode mode)
        {

        }

        private void OnSceneUnload(Scene scene)
        {
            RemoveAllEventListeners();
        }

        public static void QuitGame()
        {
            Application.Quit();
        }
    }
}