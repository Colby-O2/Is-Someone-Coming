using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.DataPersistence
{
    public interface IDataPersistence
    {
        /// <summary>
        /// Load the <paramref name="data"/>.
        /// Current implementation requires this be done between scenes.
        /// </summary>
        public bool SaveData<TData>(ref TData data) where TData : GameData;

        /// <summary>
        /// save the <paramref name="data"/>.
        /// Current implementation requires this be done between scenes.
        /// </summary>
        public bool LoadData<TData>(TData data) where TData : GameData;
    }
}
