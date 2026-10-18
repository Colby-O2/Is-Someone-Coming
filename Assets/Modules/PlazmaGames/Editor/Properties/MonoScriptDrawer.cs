using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using PlazmaGames.Attribute;
using System;
using PlazmaGames.UI;

namespace PlazmaGames.Editor
{
    public abstract class MonoScriptDrawer<T> : PropertyDrawer where T : MonoBehaviour
    {
        protected static Dictionary<string, MonoScript> _scripts;
        protected bool _viewString = false;

        static MonoScriptDrawer()
        {
            _scripts = new Dictionary<string, MonoScript>();
            MonoScript[] scripts = Resources.FindObjectsOfTypeAll<MonoScript>();
            foreach (MonoScript script in scripts)
            {
                Type scriptType = script.GetClass();

                if (scriptType != null && !_scripts.ContainsKey(scriptType.FullName)) _scripts.Add(scriptType.FullName, script);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                Rect rect = EditorGUI.PrefixLabel(position, label);
                Rect labelRect = position;
                labelRect.xMax = rect.xMin;
                position = rect;
                _viewString = GUI.Toggle(labelRect, _viewString, string.Empty, "label");
                if (_viewString)
                {
                    property.stringValue = EditorGUI.TextField(position, property.stringValue);
                    return;
                }

                MonoScript script = null;
                string typeName = property.stringValue.Split(',')[0];
                if (!string.IsNullOrEmpty(typeName))
                {
                    if (!_scripts.TryGetValue(typeName, out script))
                    {
                        Debug.LogWarning($"The script {script.name} doesn't exist.");
                        GUI.color = Color.red;
                    }
                }

                if (script != null && !typeof(T).IsAssignableFrom(script.GetClass()))
                {
                    Debug.LogWarning($"The script {script.name} is not of the correct type {typeof(T)}.");
                    GUI.color = Color.red;
                }

                script = (MonoScript)EditorGUI.ObjectField(position, script, typeof(MonoScript), false);
    
                if (GUI.changed)
                {
                    if (script != null)
                    {
                        Type type = script.GetClass();

                        MonoScriptAttribute attr = attribute as MonoScriptAttribute;
       
                        if (attr.type != null && !attr.type.IsAssignableFrom(type))
                        {
                            type = null;
                        }
                        else if (type != null)
                        {
                            property.stringValue = $"{type.FullName}, {type.Assembly.GetName().Name}";
                        }
                        else
                        {
                            Debug.LogWarning($"The script {script.name} doesn't contain an assignable class.");
                        }
                    }
                    else
                    {
                        property.stringValue = string.Empty;
                    }
                }
            }
            else
            {
                GUI.Label(position, "The MonoScriptAttribute can only be used on string variables.");
            }
        }
    }

    [CustomPropertyDrawer(typeof(ViewAttribute), false)]
    public class ViewDrawer : MonoScriptDrawer<View> { }

    [CustomPropertyDrawer(typeof(MonoBehaviourAttribute), false)]
    public class MonoBehaviourDrawer : MonoScriptDrawer<MonoBehaviour> { }
}
