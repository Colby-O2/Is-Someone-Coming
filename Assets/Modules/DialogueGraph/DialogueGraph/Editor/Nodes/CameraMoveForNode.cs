using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor.Nodes
{
    internal class CameraMoveForNode : BaseNode
    {
        public string LocationTag { get; set; }
        public string LookAtTag { get; set; }
        public float Duration { get; set; }

        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);

            title = "Camera Move For";

            Type = DialogueType.CameraMoveFor;

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node-main-container");
        }

        public override void Draw()
        {
            Port inputPort = this.CreatePort("From", direction: Direction.Input, capacity: Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            Port outputPort = this.CreatePort("To", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(outputPort);

            VisualElement customDataContainer = new VisualElement();
            customDataContainer.AddToClassList("ds-node-data-container");

            TextField locationField = EditorElementHelper.CreateTextField(
                val: LocationTag,
                label: "Location Tag",
                onValueChanged: val => LocationTag = val.newValue
            );
            locationField.AddClasses("ds-node-textfield", "ds-node-quote-textfield");
            customDataContainer.Add(locationField);

            TextField lookAtField = EditorElementHelper.CreateTextField(
                val: LookAtTag,
                label: "Look At Tag",
                onValueChanged: val => LookAtTag = val.newValue
            );
            lookAtField.AddClasses("ds-node-textfield", "ds-node-quote-textfield");
            customDataContainer.Add(lookAtField);

            FloatField durationField = new FloatField("Duration")
            {
                value = Duration
            };

            durationField.RegisterValueChangedCallback(val =>
            {
                if (val.newValue >= 0) Duration = val.newValue;
                else
                {
                    durationField.value = val.previousValue;
                    Duration = val.previousValue;
                }
            });

            durationField.AddClasses("ds-node-textfield", "ds-node-quote-textfield");
            customDataContainer.Add(durationField);

            extensionContainer.Add(customDataContainer);

            RefreshExpandedState();
        }
    }
}