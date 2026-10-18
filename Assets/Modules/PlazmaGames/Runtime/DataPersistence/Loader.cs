using PlazmaGames.Core.Debugging;
using PlazmaGames.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace PlazmaGames.DataPersistence
{
    public sealed class Loader
    {
        private string _path = "";
        private string _filename = "";
        private readonly string _fileExtension = ".json";
        private bool _encryptData;

        private string _encryptionKey;
        private SymmetricAlgorithm _key;

        public Loader(string path, string filename, bool encryptData = true, string encryptionKey = "RepalceMe")
        {
            _path = path;
            _filename = filename;
            _encryptData = encryptData;
            _encryptionKey = encryptionKey;

            if (_encryptData) CryptologyUtilities.InitializesEncryptor(ref _key, _encryptionKey);
        }

        /// <summary>
        /// Load the game data given a <paramref name="profileID"/>.
        /// </summary>
        public TData Load<TData>(string profileID) where TData : GameData
        {
            string path = Path.Combine(Application.dataPath, _path, profileID, _filename + _fileExtension);

            string data = null;

            if (File.Exists(path))
            {
                try
                {
                    using FileStream fileStream = new(path, FileMode.Open);
                    if (_encryptData)
                    {
                        using CryptoStream stream = new(
                            fileStream,
                            _key.CreateDecryptor(),
                            CryptoStreamMode.Read
                            );

                        using StreamReader reader = new(stream);
                        data = reader.ReadToEnd();
                    }
                    else
                    {
                        using StreamReader reader = new(fileStream);
                        data = reader.ReadToEnd();
                    }

                }
                catch (Exception e)
                {
                    PlazmaDebug.LogError($"An error occured loading the game data to file: {path} \n {e}", "Data Persistence", 1);
                }
            }

            return JsonUtility.FromJson<TData>(data);
        }

        /// <summary>
        /// Saves the game data to a profile with a given <paramref name="profileID"/>.
        /// </summary>
        public void Save<TData>(TData data, string profileID) where TData : GameData
        {
            string path = Path.Combine(Application.dataPath, _path, profileID, _filename + _fileExtension);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                string dataJSON = JsonUtility.ToJson(data, false);

                using FileStream fileStream = new(path, FileMode.Create);
                if (_encryptData)
                {
                    using CryptoStream stream = new(
                        fileStream,
                        _key.CreateEncryptor(),
                        CryptoStreamMode.Write
                        );
                    using StreamWriter writer = new(stream);
                    writer.Write(dataJSON);
                }
                else
                {
                    using StreamWriter writer = new(fileStream);
                    writer.Write(dataJSON);
                }
            }
            catch (Exception e)
            {
                PlazmaDebug.LogError($"An error occured loading the game data to file: {path} \n {e}", "Data Persistence", 1);
            }
        }

        /// <summary>
        /// Loads all profiles
        /// </summary>
        public Dictionary<string, TData> LoadAndGetAllProfiles<TData>() where TData : GameData
        {
            Dictionary<string, TData> profileDictionary = new();

            var dirInfos = new DirectoryInfo(
                Path.Combine(Application.dataPath, _path))
                .EnumerateDirectories();

            foreach (DirectoryInfo dir in dirInfos)
            {
                var profileID = dir.Name;

                var path = Path.Combine(Application.dataPath, _path, profileID, _filename + _fileExtension);

                if (!File.Exists(path))
                {
                    continue;
                }

                var profileData = Load<TData>(profileID);
                if (profileData != null)
                {
                    profileDictionary.Add(profileID, profileData);
                }
                else
                {
                    PlazmaDebug.LogError($"ERROR: Failed Loading ProfileID: {profileID}", "Data Persistence", 1);
                }
            }

            return profileDictionary;
        }
    }
}
