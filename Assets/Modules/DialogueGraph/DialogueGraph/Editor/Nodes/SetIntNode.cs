using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor.Nodes
{
    internal class SetIntNode : BaseNode
    {
        public int Value = 0;
        public string SelectedName;

        private PopupField<string> _dropdown;

        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);
            SetPosition(new Rect(position, Vector2.zero));
            Type = DialogueType.SetInt;
            mainContainer.AddToClassList("ds-node-main-container");
            extensionContainer.AddToClassList("ds-node-extension-container");
        }

        public void UpdateVariables(List<string> choices, string oldValue = null, string newValue = null)
        {
            _dropdown.choices = choices;

            if (oldValue != null && newValue != null && _dropdown.value == oldValue)
            {
                _dropdown.value = newValue;
            }
            else if (!choices.Contains(_dropdown.value))
            {
                _dropdown.value = null;
            }
        }

        public override void Draw()
        {
            // Title
            TextField nodeName = EditorElementHelper.CreateTextField("Set Int", isReadOnly: true);
            nodeName.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            titleContainer.Insert(0, nodeName);

            VisualElement dataContainer = new VisualElement();
            dataContainer.AddClasses("ds-node-data-container");

            if (string.IsNullOrEmpty(SelectedName))
                SelectedName = _graphView.ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).FirstOrDefault();

            _dropdown = EditorElementHelper.CreateDropdown<string>("Variable", SelectedName, _graphView.ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).ToList(), evt =>
            {
                SelectedName = evt.newValue;
                ExposedProperty selectedField = _graphView.ExposedProperties.Find(f => f.Name == SelectedName);
                userData = selectedField;
            }
             );
            SelectedName = _dropdown.value;
            _dropdown.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            dataContainer.Add(_dropdown);

            IntegerField incrementField = EditorElementHelper.CreateIntegerField("Value", Value, evt => Value = evt.newValue);
            dataContainer.Add(incrementField);

            mainContainer.Add(dataContainer);

            // Input port
            Port inputPort = this.CreatePort("From", direction: Direction.Input, capacity: Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            // Output port
            Port outputPort = this.CreatePort("To", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(outputPort);

            RefreshExpandedState();
        }
    }
}
