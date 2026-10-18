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
    internal class MultipleChoiceNode : DialogueNode
    {
        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid)
        {
            base.Initialize(position, graphView, guid);
            Type = DialogueType.MultipleChoice;
            if (Choices.Count == 0) Choices.Add("New Choice");
        }

        private Port AddChoicePort(string choice)
        {
            Port choicePort = this.CreatePort("To", direction: Direction.Output, capacity: Port.Capacity.Single);

            Choices.Add(choice);

            Button deleteButton = EditorElementHelper.CreateButton("X", () =>
            {
                _graphView.DeleteElements(choicePort.connections);
                outputContainer.Remove(choicePort);
                Choices.Remove(choice);
            });
            deleteButton.AddClasses("ds-node-button");

            TextField choiceTextField = EditorElementHelper.CreateTextField(choice);
            choiceTextField.AddClasses("ds-node-textfield", "ds-node-quote-textfield");

            choicePort.Add(choiceTextField);
            choicePort.Add(deleteButton);

            return choicePort;
        }

        public override void Draw()
        {
            base.Draw();

            Button addButton = EditorElementHelper.CreateButton("Add Choice", () => 
            {
                Port choicePort = AddChoicePort("New Choice");
                outputContainer.Add(choicePort);
            });
            addButton.AddClasses("ds-node-button");
            mainContainer.Insert(1, addButton);

            // Output
            List<string> choices = new List<string>(Choices);
            Choices.Clear();
            foreach (string choice in choices)
            {
                Port choicePort = AddChoicePort(choice);
                outputContainer.Add(choicePort);
            }

            RefreshExpandedState();
        }
    }
}
