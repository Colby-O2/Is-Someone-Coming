using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Core.Utils
{
    public sealed class Time
    {
        public static long GetCurrentTimeInMS()
        {
            return DateTime.Now.Ticks / 10000;
        }

        public static long GetCurrentTimeInS()
        {
            return DateTime.Now.Ticks / 10000000;
        }
    }
}
