using DialogueGraph.Editor.Views;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueGraph.Editor.Nodes
{
    internal class EnableCinematicCameraNode : BaseNode
    {
        public override void Initialize(Vector2 position, DialogueGraphView graphView, string guid = null)
        {
            base.Initialize(position, graphView, guid);

            title = "Enable Cinematic Camera";

            Type = DialogueType.EnableCinematicCamera;

            SetPosition(new Rect(position, Vector2.zero));

            mainContainer.AddToClassList("ds-node-main-container");
        }

        public override void Draw()
        {
            Port inputPort = this.CreatePort("From", direction: Direction.Input, capacity: Port.Capacity.Multi);
            inputContainer.Add(inputPort);

            Port outputPort = this.CreatePort("To", direction: Direction.Output, capacity: Port.Capacity.Single);
            outputContainer.Add(outputPort);

            RefreshExpandedState();
        }
    }
}
