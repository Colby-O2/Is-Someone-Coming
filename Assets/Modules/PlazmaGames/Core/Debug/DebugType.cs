using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Core.Debugging
{
    public enum DebugType
    {
        Log,
        Warning,
        Error,
        Expection
    }

    public static class DebugTypeExtension
    {
        public static string GetPrefix(this DebugType type)
        {
            return type switch
            {
                DebugType.Log => string.Empty,
                DebugType.Warning => DebugType.Warning.ToString() + " ",
                DebugType.Error => DebugType.Error.ToString() + " ",
                DebugType.Expection => DebugType.Expection.ToString() + " ",
                _ => string.Empty,
            };
        }

        public static Color GetColor(this DebugType type)
        {
            return type switch
            {
                DebugType.Log => Color.white,
                DebugType.Warning => Color.yellow,
                DebugType.Error => Color.red,
                DebugType.Expection => Color.blue,
                _ => Color.white,
            };
        }
    }
}
