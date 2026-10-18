using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Console
{
    public interface IConsoleCommand
    {
        string Command { get; }

        string Description { get; }

        bool Process(string[] args, out ConsoleResponse msg);
    }
}
