using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor
{
    [System.Serializable]
    public class ExposedProperty
    {
        public string Name;
        public string Type;
        public object Value;

        public VisualElement Container;
    }
}
