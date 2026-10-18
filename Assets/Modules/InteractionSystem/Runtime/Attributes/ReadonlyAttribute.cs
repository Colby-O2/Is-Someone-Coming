using UnityEngine;

namespace InteractionSystem.Attribute
{
    internal class ReadOnlyAttribute : PropertyAttribute
    {

    }
}

#if UNITY_EDITOR
namespace InteractionSystem.Attribute
{
    using UnityEditor;

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    internal class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label);
            GUI.enabled = true;
        }
    }

}
#endif