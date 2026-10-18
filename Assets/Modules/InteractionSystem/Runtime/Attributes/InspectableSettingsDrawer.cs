#if UNITY_EDITOR
using Codice.CM.Client.Differences;
using InteractionSystem.Settings;
using UnityEditor;
using UnityEngine;

namespace InteractionSystem.Attribute
{

    [CustomPropertyDrawer(typeof(InspectableSettings))]
    public class InspectableSettingsDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0f;
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            height += (line + spacing);
            height += (line + spacing);

            SerializedProperty lookTypeProp = property.FindPropertyRelative("LookType");
            InspectableLookType lookType = (InspectableLookType)lookTypeProp.enumValueIndex;

            if (lookType == InspectableLookType.LookAt)
            {
                height += (line + spacing);
            }
            else if (lookType == InspectableLookType.Pickup)
            {
                height += (line + spacing);

                SerializedProperty rotateProp = property.FindPropertyRelative("RotatePickup");
                if (rotateProp.boolValue)
                    height += (line + spacing);
            }

            height += (line + spacing);

            SerializedProperty allowZoomProp = property.FindPropertyRelative("AllowZoom");
            height += (line + spacing);
            if (allowZoomProp.boolValue)
                height += (line + spacing);

            height += (line + spacing);
            height += (line + spacing);
            height += (line + spacing);

            SerializedProperty itemListProp = property.FindPropertyRelative("ItemList");

            float totalHeight = line;

            if (itemListProp.isExpanded)
            {
                for (int i = 0; i < itemListProp.arraySize; i++)
                {
                    SerializedProperty element = itemListProp.GetArrayElementAtIndex(i);

                    float elementHeight = EditorGUI.GetPropertyHeight(element, true);
                    totalHeight += elementHeight + spacing;
                }
            }
            else
            {
                totalHeight += spacing;
            }

            height += totalHeight + spacing;

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float y = position.y;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = 2f;

            SerializedProperty titleProp = property.FindPropertyRelative("Title");
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), titleProp);
            y += lineHeight + spacing;

            SerializedProperty lookTypeProp = property.FindPropertyRelative("LookType");
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), lookTypeProp);
            y += lineHeight + spacing;

            InspectableLookType lookType = (InspectableLookType)lookTypeProp.enumValueIndex;

            if (lookType == InspectableLookType.LookAt)
            {
                SerializedProperty targetProp = property.FindPropertyRelative("LookAtTarget");

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), targetProp);
                y += lineHeight + spacing;
            }
            else if (lookType == InspectableLookType.Pickup)
            {
                SerializedProperty rotateProp = property.FindPropertyRelative("RotatePickup");
                SerializedProperty rotationProp = property.FindPropertyRelative("PickupRotation");

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), rotateProp);
                y += lineHeight + spacing;
                if (rotateProp.boolValue)
                {
                    EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), rotationProp);
                    y += lineHeight + spacing;
                }
            }

            SerializedProperty offsetProp = property.FindPropertyRelative("OffsetDistance");
            SerializedProperty allowZoomProp = property.FindPropertyRelative("AllowZoom");
            SerializedProperty zoomMinProp = property.FindPropertyRelative("ZoomMin");
            SerializedProperty readTextProp = property.FindPropertyRelative("ReadText");
            SerializedProperty allowRotateProp = property.FindPropertyRelative("AllowRotate");
            SerializedProperty hasInteractionProp = property.FindPropertyRelative("HasInteractions");
            SerializedProperty itemListProp = property.FindPropertyRelative("ItemList");

            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), offsetProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), allowZoomProp);
            y += lineHeight + spacing;
            if (allowZoomProp.boolValue)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), zoomMinProp);
                y += lineHeight + spacing;
            }
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), readTextProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), allowRotateProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), hasInteractionProp);
            y += lineHeight + spacing;
            EditorGUI.PropertyField(new Rect(position.x, y, position.width, lineHeight), itemListProp);
            y += lineHeight + spacing;

            EditorGUI.EndProperty();
        }
    }
}
#endif