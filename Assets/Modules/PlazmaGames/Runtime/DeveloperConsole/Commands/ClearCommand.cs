using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Console
{
    [CreateAssetMenu(fileName = "ClearCommand", menuName = "Console Commands/General/Clear")]
    public sealed class ClearCommand : ConsoleCommand
    {
        public override bool Process(string[] args, out ConsoleResponse msg)
        {
            msg = new(ResponseType.Clear);
            return true;
        }
    }
}
