using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor.Nodes
{
    internal class EmitEventNode : BaseNode
    {
        public string EventID { get; set; }

        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);
            SetPosition(new Rect(position, Vector2.zero));
            Type = DialogueType.EmitEvent;
            mainContainer.AddToClassList("ds-node-main-container");
            extensionContainer.AddToClassList("ds-node-extension-container");
        }

        public override void Draw()
        {
            // Title 
            TextField dialogueTF = EditorElementHelper.CreateTextField("Emit Event", isReadOnly: true);
            dialogueTF.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            titleContainer.Insert(0, dialogueTF);

            // Data
            VisualElement mainDataContainor = new VisualElement();
            mainDataContainor.AddClasses("ds-node-data-container");

            TextField eventTextField = EditorElementHelper.CreateTextField(val: EventID, label: "Event Tag", onValueChanged: val => EventID = val.newValue);
            eventTextField.AddClasses("ds-node-textfield", "ds-node-quote-textfield");

            mainDataContainor.Add(eventTextField);
            mainContainer.Add(mainDataContainor);

            // Input
            Port inputPort = this.CreatePort("From", direction: Direction.Input, capacity: Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            //Output
            Port outputPort = this.CreatePort("From", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(outputPort);
        }
    }
}
