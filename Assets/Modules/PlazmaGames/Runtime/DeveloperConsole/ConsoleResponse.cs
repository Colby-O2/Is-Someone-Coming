using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Console
{
    [System.Serializable]
    public sealed class ConsoleResponse
    {
        public ResponseType Type;
        public string Message = string.Empty;
        public Color MessageColor { get; private set; }

        public ConsoleResponse(ResponseType type)
        {
            Type = type;
        }

        public ConsoleResponse(string msg, ResponseType type)
        {
            Message = msg.TrimEnd(Environment.NewLine.ToCharArray());
            Type = type;
            MessageColor = GetMessageColor();
        }

        private Color GetMessageColor()
        {
            return Type switch
            {
                ResponseType.Error => Color.red,
                ResponseType.Warning => Color.yellow,
                ResponseType.Response => Color.magenta,
                ResponseType.Help => Color.magenta,
                _ => Color.white,
            };
        }
    }
}
