using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Console
{
    [CreateAssetMenu(fileName = "HelpCommand", menuName = "Console Commands/General/Help")]
    internal sealed class HelpCommand : ConsoleCommand
    {
        public override bool Process(string[] args, out ConsoleResponse msg)
        {
            msg = new(ResponseType.Help);
            return true;
        }
    }
}
