using PlazmaGames.Console;
using PlazmaGames.Core.Debugging;
using PlazmaGames.SO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.SO.Databases
{
    [CreateAssetMenu(fileName = "DeveloperConsoleDatabase", menuName = "Database/DeveloperConsole")]
    public class DeveloperConsoleDatabase : SODatabase<ConsoleCommand>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        public static void Initialize()
        {
            SODatabase<ConsoleCommand>[] databases = Resources.LoadAll<SODatabase<ConsoleCommand>>("");
            foreach (SODatabase<ConsoleCommand> database in databases) database.InitDatabase();
        }
    }
}
