using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor.Nodes
{
    internal abstract class DialogueNode : BaseNode
    {
        public string ActorName { get; set; }
        public List<string> Choices { get; set; }
        public string Text { get; set; }
        public string EventID { get; set; }

        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);

            if (Choices == null) Choices = new List<string>();

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node-main-container");
            extensionContainer.AddToClassList("ds-node-extension-container");
        }

        public override void Draw()
        {
            // Title 
            TextField dialogueTF = EditorElementHelper.CreateTextField(ActorName, onValueChanged: val => ActorName = val.newValue);
            dialogueTF.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            titleContainer.Insert(0, dialogueTF);

            // Input
            Port inputPort = this.CreatePort("From", direction: Direction.Input, capacity: Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            // Data
            VisualElement mainDataContainor = new VisualElement();
            mainDataContainor.AddClasses("ds-node-data-container");

            TextField eventTextField = EditorElementHelper.CreateTextField(val: EventID, label: "Event Tag", onValueChanged: val => EventID = val.newValue);
            eventTextField.AddClasses("ds-node-textfield", "ds-node-quote-textfield");

            mainDataContainor.Add(eventTextField);
            extensionContainer.Add(mainDataContainor);

            VisualElement dataContainor = new VisualElement();
            dataContainor.AddClasses("ds-node-data-container");

            Foldout textFoldout = EditorElementHelper.CreateFoldout("Dialogue Test");
            TextField textTextField = EditorElementHelper.CreateTextField(Text, multiline: true, onValueChanged: val => Text = val.newValue);
            textTextField.AddClasses("ds-node-textfield", "ds-node-choice-textfield", "ds-node-textfield-hidden");

            textFoldout.Add(textTextField);
            dataContainor.Add(textFoldout);
            mainContainer.Add(dataContainor);
        }
    }
}
