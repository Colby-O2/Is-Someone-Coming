using PlazmaGames.Console;
using PlazmaGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnableVerboseCommand", menuName = "Console Commands/Debug/EnableVerbose")]
public class EnableVerboseCommand : ConsoleCommand
{
    public override bool Process(string[] args, out ConsoleResponse msg)
    {
        if (args.Length == 0)
        {
            msg = new($"Need to specify an state (0 or 1).", ResponseType.Warning);
            return false;
        }

        if (int.TryParse(args[0], out int mode))
        {
            GameManager.VerboseLevel = mode;
        }
        else
        {
            msg = new($"Argument must be an of type {typeof(int)}.", ResponseType.Warning);
            return false;
        }

        msg = new(ResponseType.None);
        return true;
    }
}
