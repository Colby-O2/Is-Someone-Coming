using DialogueGraph.Editor.Nodes;
using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueGraph.Editor.Nodes
{
    internal class SingleChoiceNode : DialogueNode
    {
        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);
            Type = DialogueType.SingleChoice;
        }

        public override void Draw()
        {
            base.Draw();

            // Output
            Port choicePort = this.CreatePort("To", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(choicePort);
            
            RefreshExpandedState();
        }
    }
}