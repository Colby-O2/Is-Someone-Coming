using PlazmaGames.Audio;
using PlazmaGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.DataPersistence
{
    [System.Serializable]
    public class GameData
    {
        // General Settings
        public int profileID;

        // Audio MonoSystem Settings
        public float overallVolume = 1;
        public float musicVolume = 0.5f;
        public float soundVolume = 0.5f;
        public float ambentVolume = 0.5f;
    }

}