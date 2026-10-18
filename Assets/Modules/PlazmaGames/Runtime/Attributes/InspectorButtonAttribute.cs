using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlazmaGames.Attribute
{
    public class InspectorButtonAttribute : PropertyAttribute
    {
        public static float DEFAULT_WIDTH = 100;

        public readonly string methodName;
        public float buttonWidth { get; set; }

        public InspectorButtonAttribute(string methodName, int width = -1)
        {
            if (width <= 0) buttonWidth = DEFAULT_WIDTH;
            else buttonWidth = width;
            this.methodName = methodName;
        }
    }
}
