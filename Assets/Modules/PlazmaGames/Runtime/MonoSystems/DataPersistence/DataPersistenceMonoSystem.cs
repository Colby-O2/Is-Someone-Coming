using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlazmaGames.Core.Debugging;

namespace PlazmaGames.DataPersistence
{
    public abstract class DataPersistenceMonoSystemTemplate<TData> : MonoBehaviour, IDataPersistenceMonoSystem where TData : GameData, new()
    {
        [Header("Config")]
        [SerializeField] private string _filename;
        [SerializeField] private bool _createNewGameIfNull = true;
        [SerializeField] private bool _useEncryption = true;
        [SerializeField] private string _currentProfileName = "Development";

        private TData _gameData;
        public TData Data
        {
            get => _gameData;
            private set => _gameData = value;
        }

        private List<IDataPersistence> _dataPersistencesObjects;
        private Loader _loader;

        public int ProfileID {get; private set;}

        public bool IsGameLoaded()
        {
            return _gameData != null;
        }

        public void NewGame()
        {
            PlazmaDebug.Log($"Creating a new profile for {_currentProfileName}.", "Data Persistence", Color.green, 2);
            _gameData = new TData();
        }

        public void LoadGame(bool forceNewGameWhenNull = false, bool trigger = true)
        {
            PlazmaDebug.Log($"Attempting to load profile {_currentProfileName}.", "Data Persistence", Color.green, 2);

            _gameData = _loader.Load<TData>(_currentProfileName);

            if (_gameData == null && (_createNewGameIfNull || forceNewGameWhenNull))
            {
                NewGame();
            }
            else if (_gameData == null)
            {
                PlazmaDebug.LogWarning($"Failed to load game since {_currentProfileName} does not exist.", "Data Persistence", 1);
                return;
            }

            if (trigger) foreach (IDataPersistence obj in _dataPersistencesObjects) obj.LoadData(_gameData);
        }

        public void SaveGame()
        {
            if (_gameData == null)
            {
                PlazmaDebug.LogWarning($"TData is null. Please call NewGame() before SaveGame().", "Data Persistence", 1, Color.yellow);
                return;
            }

            PlazmaDebug.Log($"Attempting to save profile {_gameData.profileID}.", "Data Persistence", Color.green, 2);
            _gameData.profileID = ProfileID;

            foreach (IDataPersistence obj in _dataPersistencesObjects) obj.SaveData(ref _gameData);

            _loader.Save(_gameData, _currentProfileName);
        }

        private List<IDataPersistence> FindAllDataPersistenceObjects()
        {
            //TODO: Find a way to get all objects with IDataPersistence without using FindObjectsOfType
            //      This is a very expensive operation.
            return FindObjectsByType<MonoBehaviour>().OfType<IDataPersistence>().ToList();
        }

        public void DeleteGame(string profileName = "")
        {
            if (profileName == string.Empty) profileName = _currentProfileName;

            PlazmaDebug.Log($"Attempting to delete {profileName}.", "Data Persistence", Color.green, 2);

            string path = Path.Combine(Application.dataPath, Application.persistentDataPath, profileName);
            if (Directory.Exists(path)) Directory.Delete(path, true);
            else PlazmaDebug.LogWarning($"Trying to delete path that does not exist:\n{path}", "Data Persistence", 1);

            _gameData = null;
        }

        public void SetProfileID(int profileID)
        {
            PlazmaDebug.Log($"Setting active profile id to {profileID}.", "Data Persistence", Color.green, 2);
            ProfileID = profileID;
        }

        public void ChangeProfile(string profileName)
        {
            PlazmaDebug.Log($"Setting active profile to {profileName}.", "Data Persistence", Color.green, 2);
            _currentProfileName = profileName;
            LoadGame();
        }

        public Dictionary<string, GameData> FetchAllProfiles()
        {
            Dictionary<string, TData>  profiles = _loader.LoadAndGetAllProfiles<TData>();

            Dictionary<string, GameData> data = new Dictionary<string, GameData>();   

            foreach (var profile in profiles)
            {
                data.Add(profile.Key, profile.Value);
            }

            return data;
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_loader == null) return;
            _dataPersistencesObjects = FindAllDataPersistenceObjects();
            LoadGame();
        }

        public void OnSceneUnloaded(Scene scene)
        {
            SaveGame();
        }

        public GameData GetGameData()
        {
            return _gameData;
        }

        private void AddListeners()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void RemoveListeners()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnEnable()
        {
            AddListeners();
        }

        private void OnDisable()
        {
            RemoveListeners();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void Awake()
        {
            Debug.Log(Application.persistentDataPath);
            PlazmaDebug.Log($"Data Persistence Manager is Awaking.", "Data Persistence", Color.green, 2);
            _loader = new Loader(Application.persistentDataPath, _filename, _useEncryption);
        }
    }
}
