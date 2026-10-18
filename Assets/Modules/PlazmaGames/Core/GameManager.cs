using System;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.SceneManagement;

using PlazmaGames.Core.Debugging;
using PlazmaGames.Core.Events;
using PlazmaGames.Core.MonoSystem;
using PlazmaGames.Settings;

namespace PlazmaGames.Core
{
	public abstract class GameManager : MonoBehaviour
	{
        protected static GameManager _instance;

        private readonly MonoSystemManager _monoSystemManager = new MonoSystemManager();
        private readonly EventManager _eventManager = new EventManager();

        [Header("Developer Settings")]
        [SerializeField] private int _verbose;

        public static GameManager Instance { get => _instance; }
        public static bool InDebugMode { get => _instance._verbose > 0; }
        public static int VerboseLevel { get => Mathf.Max(0, _instance._verbose); set => _instance._verbose = Mathf.Max(0, value); }

        /// <summary>
        /// Add a MonoSystems to the GameManager. A MonoSystem takes the place of other singleton classes.
        /// </summary>
        public static void AddMonoSystem<TMonoSystem, TBindTo>(TMonoSystem monoSystem) where TMonoSystem : IMonoSystem, TBindTo => _instance._monoSystemManager.AddMonoSystem<TMonoSystem, TBindTo>(monoSystem);

        /// <summary>
        /// Removes a MonoSystems to the GameManager. A MonoSystem takes the place of other singleton classes.
        /// </summary>
        public static void RemoveMonoSystem<TMonoSystem>() where TMonoSystem : IMonoSystem => _instance._monoSystemManager.RemoveMonoSystem<TMonoSystem>();


        /// <summary>
        /// Fetches an attached MonoSystem of type TMonoSystem.
        /// </summary>
        public static TMonoSystem GetMonoSystem<TMonoSystem>() => _instance._monoSystemManager.GetMonoSystem<TMonoSystem>();

        /// <summary>
        /// Checks if a MonoSystem is attached to the GameManager.
        /// </summary>
        public static bool HasMonoSystem<TMonoSystem>() where TMonoSystem : IMonoSystem => _instance._monoSystemManager.HasMonoSystem<TMonoSystem>();

        /// <summary>
        /// Adds an event listener to an event of type TEvent
        /// </summary>
        public static void AddEventListener<TEvent>(EventResponse listener) => _instance._eventManager.AddListener(typeof(TEvent).Name, listener);

		/// <summary>
		/// Removes an event listener to an event of type TEvent
		/// </summary>
		public static void RemoveEventListener<TEvent>(EventResponse listener) => _instance._eventManager.RemoveListener(typeof(TEvent).Name, listener);

        /// <summary>
        /// Removes all event listeners
        /// </summary>
        public static void RemoveAllEventListeners() => _instance._eventManager.RemoveAllListener();

        /// <summary>
        /// Emits a game event of type TEvent.
        /// </summary>
        public static void EmitEvent(object data, Component sender = null) => _instance._eventManager.Emit(data.GetType().Name, sender, data);

		/// <summary>
		/// Checks if a game event of type TEvent exists.
		/// </summary>
        public static bool HasEvent<TEvent>() => _instance._eventManager.HasEvent(typeof(TEvent).Name);

		/// <summary>
		/// Initialzes the GameManager automatically on scene load.
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Initialize()
		{
			if (_instance) return;

			PlazmaGamesSettings settings = PlazmaGamesSettings.GetSettings();
			string prefabPath = (settings != null) ? settings.GetSceneGameManagerNameOrDefault(SceneManager.GetActiveScene().name) : "GameManager";

			GameManager gameManagerPrefab = Resources.Load<GameManager>(prefabPath);
            if (gameManagerPrefab == null)
            {
                PlazmaDebug.LogWarning("GameManager Prefab is Null!", "GameManager", 0, Color.yellow);
                return;
            }
			GameManager gameManager = Instantiate(gameManagerPrefab);

			gameManager.name = gameManager.GetApplicationName();

			DontDestroyOnLoad(gameManager);

			_instance = gameManager;

            gameManager.MonoSystemInitliation();
            gameManager.OnInitalized();
		}

        /// <summary>
        /// Fetches the name of the application.
        /// </summary>
        public abstract string GetApplicationName();

        /// <summary>
        /// Fetches the version of the application.
        /// </summary>
        public abstract string GetApplicationVersion();

        private void MonoSystemInitliation()
        {
            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            IMonoSystem[] systems = behaviours.OfType<IMonoSystem>().ToArray();
            foreach (IMonoSystem system in systems)
            {
                RegisterMonoSystem(system);
            }
        }

        private void RegisterMonoSystem(IMonoSystem system)
        {
            Type systemType = system.GetType();
            Type interfaceType = systemType.GetInterface($"I{systemType.Name}");

            if (interfaceType != null)
            {
                MethodInfo method = typeof(GameManager).GetMethod(nameof(AddMonoSystem), BindingFlags.Public | BindingFlags.Static);

                if (method != null)
                {
                    MethodInfo genericMethod = method.MakeGenericMethod(systemType, interfaceType);

                    genericMethod.Invoke(null, new object[] { system });

                    PlazmaDebug.Log($"Registered {systemType.Name} as {interfaceType.Name}", "GameManager", color: Color.green, verboseLevel: 1);
                }
            }
        }

        /// <summary>
        /// <summary>
        /// Function to be ran after the GameManager is Initalized.
        /// </summary>
        protected abstract void OnInitalized();
	}
}
