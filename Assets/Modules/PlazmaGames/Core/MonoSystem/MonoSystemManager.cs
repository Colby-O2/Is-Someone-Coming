using PlazmaGames.Core.Debugging;
using System;
using System.Collections.Generic;

namespace PlazmaGames.Core.MonoSystem
{
    internal sealed class MonoSystemManager 
    {
        private readonly Dictionary<Type, IMonoSystem> _monoSystems = new Dictionary<Type, IMonoSystem>();

        /// <summary>
        /// Adds a MonoSystem to the master list.
        /// </summary>
        public void AddMonoSystem<TMonoSystem, TBindTo>(TMonoSystem monoSystem) where TMonoSystem : TBindTo, IMonoSystem
        {
            if (monoSystem == null) throw new Exception($"{nameof(monoSystem)} cannot be null!!!");
            Type monoSystemType = typeof(TBindTo);
            if (!_monoSystems.ContainsKey(monoSystemType)) _monoSystems[monoSystemType] = monoSystem;
        }

        /// <summary>
        /// Removes a MonoSystem to the master list.
        /// </summary>
        public void RemoveMonoSystem<TMonoSystem>() where TMonoSystem : IMonoSystem
        {
            if (HasMonoSystem<TMonoSystem>()) _monoSystems.Remove(typeof(TMonoSystem));
            else PlazmaDebug.LogWarning($"Trying to remove MonoSystem {typeof(IMonoSystem)} which is not currently attached", "MonoSystem", 1);
        }

        /// <summary>
        /// Removes a MonoSystem from the master list.
        /// </summary>
        public TMonoSystem GetMonoSystem<TMonoSystem>()
        {
            Type monoSystemType = typeof(TMonoSystem);

            if (_monoSystems.TryGetValue(monoSystemType, out var monoSystem)) return (TMonoSystem)monoSystem;
            else
            {
                Exception e = new Exception($"MonoSystem {monoSystemType} does not exist");
                PlazmaDebug.LogExpection(e, $"MonoSystem {monoSystemType} does not exist", "MonoSystem");
                throw e;
            }
        }

        /// <summary>
        /// Checks if a MonoSystem is attached.
        /// </summary>
        public bool HasMonoSystem<TMonoSystem>() where TMonoSystem : IMonoSystem
        {
            Type monoSystemType = typeof(TMonoSystem);
            return _monoSystems.ContainsKey(monoSystemType);
        }
    }
}
