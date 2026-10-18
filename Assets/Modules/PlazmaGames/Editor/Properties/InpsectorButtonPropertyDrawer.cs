using PlazmaGames.Attribute;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PlazmaGames.Editor
{
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InpsectorButtonPropertyDrawer : PropertyDrawer
    {
        private MethodInfo _methodInfo = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InspectorButtonAttribute attr = (InspectorButtonAttribute)attribute;
            Rect buttonRect = new Rect(position.x + (position.width - attr.buttonWidth) * 0.5f, position.y, attr.buttonWidth, position.height);
            if (GUI.Button(buttonRect, label.text))
            {
                System.Type parnetType = property.serializedObject.targetObject.GetType();
                string methodName = attr.methodName;

                if (_methodInfo == null)
                    _methodInfo = parnetType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (_methodInfo != null)
                    _methodInfo.Invoke(property.serializedObject.targetObject, null);
                else
                    Debug.LogWarning($"Unable to find method {methodName} in {parnetType}.");
            }
        }
    }
}
