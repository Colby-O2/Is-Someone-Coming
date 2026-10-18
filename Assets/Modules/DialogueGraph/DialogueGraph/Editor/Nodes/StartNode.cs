using DialogueGraph.Editor.Nodes;
using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor.Nodes
{
    internal class StartNode : BaseNode
    {
        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);

            SetPosition(new Rect(position, Vector2.zero));
            Type = DialogueType.Start;
            mainContainer.AddToClassList("ds-node-main-container");
            extensionContainer.AddToClassList("ds-node-extension-container");
        }

        public override void Draw()
        {
            // Title 
            TextField nodeName = EditorElementHelper.CreateTextField("Start", isReadOnly: true);
            nodeName.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            titleContainer.Insert(0, nodeName);

            // Output
            Port outputPort = this.CreatePort("To", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(outputPort);

            RefreshExpandedState();
        }
    }
}