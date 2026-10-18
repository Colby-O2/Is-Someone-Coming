using DialogueGraph.Enumeration;
using DialogueGraph.SO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DialogueGraph.Data
{
    [CreateAssetMenu(fileName = "NewDialogueGraph", menuName = "Dialogue/Graph")]
    public class DialogueGraphSO : BaseSO
    {
        public string Name;
        public List<DialogueNodeData> Nodes = new List<DialogueNodeData>();
        public List<DialogueGroupData> Groups = new List<DialogueGroupData>();
        public List<DialogueExposedPropertiesData> Properties = new List<DialogueExposedPropertiesData>();

    }

    [System.Serializable]
    public class DialogueChoiceData
    {
        public string Text;
        public string Next;
    }

    [System.Serializable]
    public class DialogueGroupData
    {
        public string Name;
        public Vector2 Position;
    }

    [System.Serializable]
    public class DialogueExposedPropertiesData
    {
        public string Name;
        public string Type;
        public bool BooleanValue;
        public int IntValue;

    }

    [System.Serializable]
    public class DialogueNodeData
    {
        public string Guid;
        public string ActorName;
        public string GroupName;
        public DialogueType Type;
        public string Text;
        public string EventID;
        public Vector2 position;
        public string PropertyType;
        public bool PropertyBooleanValue;
        public int PropertyIntValue;
        public string PropertyName;
        public int IncrementAmount;
        public ComparisonType ComparisonType;
        public int ComparsionCondition;
        public string CameraFromLocationTag;
        public string CameraToLocationTag;
        public string CameraLookAtTag;
        public float Duration;
        public List<DialogueChoiceData> Choices = new List<DialogueChoiceData>();
    }
}
