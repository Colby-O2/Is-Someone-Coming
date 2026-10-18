using PlazmaGames.Core.MonoSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.DataPersistence
{
    public interface IDataPersistenceMonoSystem : IMonoSystem
    {
        /// <summary>
        /// Crates a new game.
        /// </summary>
        public void NewGame();

        /// <summary>
        /// Save the game to an JSON file
        /// </summary>
        public void LoadGame(bool forceNewGameWhenNull = false, bool trigger = true);

        /// <summary>
        /// Loads a game from JSON file
        /// </summary>
        public void SaveGame();

        public bool IsGameLoaded();

        public void DeleteGame(string profileName = "");

        public void SetProfileID(int profileID);

        public void ChangeProfile(string profileName);

        public GameData GetGameData();

        /// <summary>
        /// Loads all profiles
        /// </summary>
        public Dictionary<string, GameData> FetchAllProfiles();
    }
}
