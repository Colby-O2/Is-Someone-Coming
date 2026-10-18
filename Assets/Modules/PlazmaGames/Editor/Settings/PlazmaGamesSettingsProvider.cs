using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PlazmaGames.Settings;

namespace PlazmaGames.Editor.Settings
{
    internal class PlazmaGamesSettingsProvider : SettingsProvider
    {
        private SerializedObject _plazmaGamesSettings;

        class Styles
        {
            public static GUIContent defaultGameManagerName = new GUIContent("Default GameManager Name");
            public static GUIContent sceneSpecificGameManagers = new GUIContent("Scene Specific GameManagers");
        }

        public PlazmaGamesSettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public static bool IsSettingsAvailable()
        {
            return File.Exists(PlazmaGamesSettings.path);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _plazmaGamesSettings = PlazmaGamesSettings.GetSerializedSettings();
        }

        public override void OnGUI(string searchContext)
        {
            _plazmaGamesSettings.Update();
            EditorGUILayout.PropertyField(_plazmaGamesSettings.FindProperty("_defaultGameManagerName"), Styles.defaultGameManagerName);
            EditorGUILayout.PropertyField(_plazmaGamesSettings.FindProperty("_sceneSpecificGameManagerEntries"), Styles.sceneSpecificGameManagers);
            _plazmaGamesSettings.ApplyModifiedProperties();
        }

        [SettingsProvider]
        public static SettingsProvider CreatePlazmaGamesSettingsProvider()
        {
            if (IsSettingsAvailable())
            {
                PlazmaGamesSettingsProvider provider = new PlazmaGamesSettingsProvider("Project/Plazma Games GameManager Settings", SettingsScope.Project);

                provider.keywords = GetSearchKeywordsFromGUIContentProperties<Styles>();
                return provider;
            }

            return null;
        }
    }
}
