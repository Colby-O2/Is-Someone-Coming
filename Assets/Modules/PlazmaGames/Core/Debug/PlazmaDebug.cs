using PlazmaGames.Core;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace PlazmaGames.Core.Debugging
{
    public static class PlazmaDebug
    {
        public static UnityEvent<string, DebugType, Color?, int> OnDebug = new UnityEvent<string, DebugType, Color?, int>();

        private static string ConstructLog(string msg, string quilfier = "", Color? color = null)
        {
            return (color == null) ? $"{quilfier}: {msg}" : $"<color=#{ColorUtility.ToHtmlStringRGBA(color ?? Color.white)}>{quilfier}:</color> {msg}";
        }

        public static bool CanLog(int verboseLevel)
        {
            return GameManager.Instance == null || (GameManager.InDebugMode && GameManager.VerboseLevel >= verboseLevel);
        }

        public static void Log(string msg, string quilfier = "", Color? color = null, int verboseLevel = 0)
        {
            string log = ConstructLog(msg, quilfier, color);
            OnDebug?.Invoke(log, DebugType.Log, color, verboseLevel);

#if UNITY_EDITOR
            if (CanLog(verboseLevel)) Debug.Log(log); 
#endif
        }

        public static void LogWarning(string msg, string quilfier = "", int verboseLevel = 0, Color? color = null)
        {
            color ??= Color.yellow;
            string log = ConstructLog(msg, quilfier, color);
            OnDebug?.Invoke(log, DebugType.Warning, color, verboseLevel);

#if UNITY_EDITOR
            if (CanLog(verboseLevel)) Debug.LogWarning(log);
#endif
        }

        public static void LogExpection(Exception e, string msg = "", string quilfier = "", int verboseLevel = 0, Color? color = null)
        {
            color ??= Color.red;
            string log = ConstructLog(msg + e.ToString(), quilfier, color);
            OnDebug?.Invoke(log, DebugType.Expection, color, verboseLevel);

#if UNITY_EDITOR
            if (CanLog(verboseLevel)) Debug.LogError(log);
#endif
        }

        public static void LogError(string msg, string quilfier = "", int verboseLevel = 0, Color? color = null)
        {
            color ??= Color.red;
            string log = ConstructLog(msg, quilfier, color);
            OnDebug?.Invoke(log, DebugType.Error, color, verboseLevel);

#if UNITY_EDITOR
            if (CanLog(verboseLevel)) Debug.LogError(log);
#endif
        }
    }
}
