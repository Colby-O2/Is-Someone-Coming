#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueGraph.Utilities
{
    public static class EditorElementHelper
    {
        public static TextField CreateTextField(string val = null, string label = null, bool isReadOnly = false, bool multiline = false, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textField = new TextField();
            textField.value = val;
            textField.label = label;
            textField.multiline = multiline;
            textField.isReadOnly = isReadOnly;

            if (onValueChanged != null) textField.RegisterValueChangedCallback(onValueChanged);

            return textField;
        }

        public static TextField CreateTextArea(string val = null, string label = null, EventCallback<ChangeEvent<string>> onValueChanged = null)
        {
            TextField textArea = CreateTextField(val, label, false, false, onValueChanged);
            textArea.multiline = true;
            return textArea;
        }

        public static Foldout CreateFoldout(string title, bool collapsed = false)
        {
            Foldout foldout = new Foldout();
            foldout.text = title;
            foldout.value = !collapsed;
            return foldout;
        }

        public static Button CreateButton(string text, Action onClick = null)
        {
            Button button = new Button(onClick);
            button.text = text;
            return button;
        }


        public static Toggle CreateToggle(string label, bool value = false, EventCallback<ChangeEvent<bool>> onValueChanged = null)
        {
            Toggle toggle = new Toggle(label);
            toggle.value = value;
            toggle.RegisterValueChangedCallback(evt => onValueChanged?.Invoke(evt));
            return toggle;
        }

        public static PopupField<T> CreateDropdown<T>(string label, T defaultValue = default, List<T> options = null, EventCallback<ChangeEvent<T>> onValueChanged = null)
        {
            PopupField<T> dropdown = new PopupField<T>(options, 0);
            dropdown.label = label;
            dropdown.value = defaultValue;
            dropdown.RegisterValueChangedCallback(onValueChanged);
            return dropdown;
        }

        public static IntegerField CreateIntegerField(string label, int defaultValue = 0, EventCallback<ChangeEvent<int>> onValueChanged = null)
        {
            IntegerField field = new IntegerField(label) { value = defaultValue };
            field.RegisterValueChangedCallback(onValueChanged);
            return field;
        }

        public static Port CreatePort(this Node node, string portName = "", Orientation orientation = Orientation.Horizontal, Direction direction = Direction.Output, Port.Capacity capacity = Port.Capacity.Single)
        {
            Port port = node.InstantiatePort(orientation, direction, capacity, typeof(bool));
            port.portName = portName;
            return port;
        }
    }
}
#endif
