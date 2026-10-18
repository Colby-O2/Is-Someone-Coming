using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Editor.Nodes
{
    internal class ComparatorNode : BaseNode
    {
        public string SelectedName;
        public int Condition;
        public ComparisonType ComparisonType;

        private PopupField<string> _dropdown;
        private PopupField<string> _comparsionDropDown;

        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);
            SetPosition(new Rect(position, Vector2.zero));
            Type = DialogueType.Comparator;
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
            TextField nodeName = EditorElementHelper.CreateTextField("Comparator", isReadOnly: true);
            nodeName.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            titleContainer.Insert(0, nodeName);

            VisualElement dataContainor = new VisualElement();
            dataContainor.AddClasses("ds-node-data-container");

            if (string.IsNullOrEmpty(SelectedName)) SelectedName = _graphView.ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).FirstOrDefault();
            _dropdown = EditorElementHelper.CreateDropdown<string>("Variable", SelectedName, _graphView.ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).ToList(), evt =>
            {
                SelectedName = evt.newValue;
                ExposedProperty selectedField = _graphView.ExposedProperties.Find(f => f.Name == SelectedName);
                userData = selectedField;
            });
            SelectedName = _dropdown.value;
            _dropdown.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            dataContainor.Add(_dropdown);


            _comparsionDropDown = EditorElementHelper.CreateDropdown<string>("Comparison", ComparisonType.ToString(), Enum.GetNames(typeof(ComparisonType)).ToList(), evt =>
            {
                ComparisonType = (ComparisonType)Enum.Parse(typeof(ComparisonType), evt.newValue);
            });
            ComparisonType = (ComparisonType)Enum.Parse(typeof(ComparisonType), _comparsionDropDown.value);
            _comparsionDropDown.AddClasses("ds-node-textfield", "ds-node-textfield-filename", "ds-node-textfield-hidden");
            dataContainor.Add(_comparsionDropDown);

            IntegerField incrementField = EditorElementHelper.CreateIntegerField("Condition", Condition, evt => Condition = evt.newValue);
            dataContainor.Add(incrementField);

            mainContainer.Add(dataContainor);

            // Input
            Port inputPort = this.CreatePort("From", direction: Direction.Input, capacity: Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            // Output
            Port falseOutputPort = this.CreatePort("False", direction: Direction.Output, capacity: Port.Capacity.Single);
            Port trueOutputPort = this.CreatePort("True", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(falseOutputPort);
            outputContainer.Add(trueOutputPort);

            RefreshExpandedState();
        }
    }
}
