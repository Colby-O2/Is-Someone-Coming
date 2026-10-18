using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlazmaGames.Settings
{
    public class PlazmaGamesSettings : ScriptableObject
    {
        public const string pathToAsset = "Settings/PlazmaGamesSettings";
        public const string pathToResources = "Assets/Resources/";
        public const string extension = ".asset";
        public static string path => pathToResources + pathToAsset + extension;

        [SerializeField] private string _defaultGameManagerName;
        [SerializeField] private List<SceneSpecificGameManagerEntry> _sceneSpecificGameManagerEntries;

#if UNITY_EDITOR
        private static void Initialize(PlazmaGamesSettings settings)
        {
            settings._defaultGameManagerName = "GameManager";
            settings._sceneSpecificGameManagerEntries = new List<SceneSpecificGameManagerEntry>();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
        }

        public static PlazmaGamesSettings GetOrCreateSettings()
        {
            PlazmaGamesSettings settings = AssetDatabase.LoadAssetAtPath<PlazmaGamesSettings>(path);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlazmaGamesSettings>();
                Initialize(settings);
            }

            return settings;
        }

        public static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }
#endif

        public static PlazmaGamesSettings GetSettings()
        {
            return Resources.Load<PlazmaGamesSettings>(pathToAsset);
        }

        public string GetDefaultGameManagerName() => _defaultGameManagerName;

        public string GetSceneGameManagerName(string sceneName) => _sceneSpecificGameManagerEntries.FirstOrDefault(e => e.sceneName.CompareTo(sceneName) == 0).gameManagerName;

        public string GetSceneGameManagerNameOrDefault(string sceneName) => _sceneSpecificGameManagerEntries.FirstOrDefault(e => e.sceneName.CompareTo(sceneName) == 0)?.gameManagerName ?? GetDefaultGameManagerName();
    }
}
