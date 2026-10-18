using System;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using DialogueGraph.Enumeration;
using DialogueGraph.Editor.Views;

namespace DialogueGraph.Editor.Nodes
{
    internal abstract class BaseNode : Node
    {
        protected DialogueGraphView _graphView;
        protected string _id;
        protected Vector2 _defaultNodeSize = new Vector2(200, 250);

        public string ID { get; set; }
        public DialogueType Type { get; set; }

        public virtual void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null) 
        {
            _graphView = graphView;
            ID = guid ?? Guid.NewGuid().ToString();
        }

        public abstract void Draw();
    }
}
