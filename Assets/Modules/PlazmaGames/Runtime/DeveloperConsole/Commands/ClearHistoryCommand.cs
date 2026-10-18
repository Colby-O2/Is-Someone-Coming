using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Console
{
    [CreateAssetMenu(fileName = "ClearHistoryCommand", menuName = "Console Commands/General/ClearHistory")]
    public sealed class ClearHistoryCommand : ConsoleCommand
    {
        public override bool Process(string[] args, out ConsoleResponse msg)
        {
            msg = new(ResponseType.ClearHistory);
            return true;
        }
    }
}
