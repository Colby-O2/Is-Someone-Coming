#if UNITY_EDITOR
using InteractionSystem.Actions;
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace InteractionSystem.Attribute
{

    [CustomEditor(typeof(Interactable))]
    internal class InteractableEditor : UnityEditor.Editor
    {
        private int _currentSlot = 0;
        private readonly string[] _slotNames = { "Slot 1", "Slot 2", "Slot 3", "Slot 4" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, "_actions");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Interaction Actions", EditorStyles.boldLabel);

            SerializedProperty actionsProp = serializedObject.FindProperty("_actions");

            if (actionsProp.arraySize != 4)
                actionsProp.arraySize = 4;

            _currentSlot = GUILayout.Toolbar(_currentSlot, _slotNames);
            EditorGUILayout.Space();

            SerializedProperty element = actionsProp.GetArrayElementAtIndex(_currentSlot);

            if (element != null)
            {
                Type[] types = TypeCache.GetTypesDerivedFrom<InteractionAction>().ToArray();

                string[] displayNames = types.Select(t => t.Name).Prepend("None").ToArray();

                string[] qualifiedNames = types.Select(t => t.AssemblyQualifiedName).Prepend(null).ToArray();

                int index = element.managedReferenceValue != null
                    ? Array.IndexOf(qualifiedNames, element.managedReferenceValue.GetType().AssemblyQualifiedName)
                    : 0;

                int newIndex = EditorGUILayout.Popup("Action Type", index, displayNames);

                if (newIndex != index)
                {
                    if (newIndex == 0)
                    {
                        element.managedReferenceValue = null;
                    }
                    else
                    {
                        Type typeToCreate = Type.GetType(qualifiedNames[newIndex]);
                        if (typeToCreate != null)
                        {
                            element.managedReferenceValue = Activator.CreateInstance(typeToCreate);
                        }
                    }

                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();
                }

                if (element.managedReferenceValue != null)
                {
                    EditorGUILayout.Space();
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(element, GUIContent.none, true);
                    EditorGUI.indentLevel--;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif