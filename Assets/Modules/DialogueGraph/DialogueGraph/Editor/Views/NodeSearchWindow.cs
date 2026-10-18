using DialogueGraph.Editor.Nodes;
using DialogueGraph.Enumeration;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueGraph.Editor.Views
{
    internal sealed class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private DialogueGraphView _graphView;

        public void Initialize(DialogueGraphView view)
        {
            _graphView = view;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            List<SearchTreeEntry> entries = new List<SearchTreeEntry>()
            {
                new SearchTreeGroupEntry(new GUIContent("Create Element")),
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 1),
                new SearchTreeGroupEntry(new GUIContent("Dialogue Nodes"), 2),
                new SearchTreeEntry(new GUIContent("Single Choice"))
                {
                    level = 3,
                    userData = DialogueType.SingleChoice,
                },
                new SearchTreeEntry(new GUIContent("Multiple Choice"))
                {
                    level = 3,
                    userData = DialogueType.MultipleChoice,
                },
                new SearchTreeGroupEntry(new GUIContent("Logic Nodes"), 2),
                new SearchTreeEntry(new GUIContent("Set Boolean"))
                {
                    level = 3,
                    userData = DialogueType.SetBoolean,
                },
                new SearchTreeEntry(new GUIContent("Branch"))
                {
                    level = 3,
                    userData = DialogueType.Branch,
                },
                new SearchTreeEntry(new GUIContent("SetInt"))
                {
                    level = 3,
                    userData = DialogueType.SetInt,
                },
                new SearchTreeEntry(new GUIContent("Increment"))
                {
                    level = 3,
                    userData = DialogueType.Increment,
                },
                new SearchTreeEntry(new GUIContent("Comparator"))
                {
                    level = 3,
                    userData = DialogueType.Comparator,
                },
                new SearchTreeEntry(new GUIContent("Wait For Seconds"))
                {
                    level = 3,
                    userData = DialogueType.WaitForSeconds,
                },
                new SearchTreeGroupEntry(new GUIContent("Helpers Nodes"), 2),
                new SearchTreeEntry(new GUIContent("Connector"))
                {
                    level = 3,
                    userData = DialogueType.Connector,
                },
                new SearchTreeGroupEntry(new GUIContent("Event Nodes"), 2),
                new SearchTreeEntry(new GUIContent("Emit Event"))
                {
                    level = 3,
                    userData = DialogueType.EmitEvent,
                },
                new SearchTreeGroupEntry(new GUIContent("Camera Nodes"), 2),
                new SearchTreeEntry(new GUIContent("Enable Cinematic Camera"))
                {
                    level = 3,
                    userData = DialogueType.EnableCinematicCamera,
                },
                new SearchTreeEntry(new GUIContent("Disable Cinematic Camera"))
                {
                    level = 3,
                    userData = DialogueType.DisableCinematicCamera,
                },
                new SearchTreeEntry(new GUIContent("Camera Move"))
                {
                    level = 3,
                    userData = DialogueType.CameraMove,
                },
                new SearchTreeEntry(new GUIContent("Camera Move For"))
                {
                    level = 3,
                    userData = DialogueType.CameraMoveFor,
                },
                new SearchTreeEntry(new GUIContent("Camera Transition"))
                {
                    level = 3,
                    userData = DialogueType.CameraTransition,
                },
                new SearchTreeEntry(new GUIContent("Camera Look At"))
                {
                    level = 3,
                    userData = DialogueType.CameraLookAt,
                },
                new SearchTreeGroupEntry(new GUIContent("Create Group"), 1),
                new SearchTreeEntry(new GUIContent("Single Group"))
                {
                    level = 2,
                    userData = new Group(),
                },
            };

            return entries;
        }

        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            Vector2 mousePosition = _graphView.GetLocalMousePosition(context.screenMousePosition, true);

            switch (SearchTreeEntry.userData)
            {
                case DialogueType.SingleChoice:
                    BaseNode singlecChoiceNode = _graphView.CreateNode(DialogueType.SingleChoice, mousePosition);
                    _graphView.AddElement(singlecChoiceNode);
                    return true;
                case DialogueType.MultipleChoice:
                    BaseNode multipleChoiceNode = _graphView.CreateNode(DialogueType.MultipleChoice, mousePosition);
                    _graphView.AddElement(multipleChoiceNode);
                    return true;
                case DialogueType.SetBoolean:
                    BaseNode setBooleanNode = _graphView.CreateNode(DialogueType.SetBoolean, mousePosition);
                    _graphView.AddElement(setBooleanNode);
                    return true;
                case DialogueType.Branch:
                    BaseNode branchNode = _graphView.CreateNode(DialogueType.Branch, mousePosition);
                    _graphView.AddElement(branchNode);
                    return true;
                case DialogueType.Increment:
                    BaseNode incrementNode = _graphView.CreateNode(DialogueType.Increment, mousePosition);
                    _graphView.AddElement(incrementNode);
                    return true;
                case DialogueType.Comparator:
                    BaseNode comparatorNode = _graphView.CreateNode(DialogueType.Comparator, mousePosition);
                    _graphView.AddElement(comparatorNode);
                    return true;
                case DialogueType.Connector:
                    BaseNode connectorNode = _graphView.CreateNode(DialogueType.Connector, mousePosition);
                    _graphView.AddElement(connectorNode);
                    return true;
                case DialogueType.SetInt:
                    BaseNode setIntNode = _graphView.CreateNode(DialogueType.SetInt, mousePosition);
                    _graphView.AddElement(setIntNode);
                    return true;
                case DialogueType.EmitEvent:
                    BaseNode emitEventNode = _graphView.CreateNode(DialogueType.EmitEvent, mousePosition);
                    _graphView.AddElement(emitEventNode);
                    return true;
                case DialogueType.EnableCinematicCamera:
                    BaseNode enableCinematicCamera = _graphView.CreateNode(DialogueType.EnableCinematicCamera, mousePosition);
                    _graphView.AddElement(enableCinematicCamera);
                    return true;
                case DialogueType.DisableCinematicCamera:
                    BaseNode disableCinematicCamera = _graphView.CreateNode(DialogueType.DisableCinematicCamera, mousePosition);
                    _graphView.AddElement(disableCinematicCamera);
                    return true;
                case DialogueType.CameraMove:
                    BaseNode cameraMoveNode = _graphView.CreateNode(DialogueType.CameraMove, mousePosition);
                    _graphView.AddElement(cameraMoveNode);
                    return true;
                case DialogueType.CameraMoveFor:
                    BaseNode cameraMoveForNode = _graphView.CreateNode(DialogueType.CameraMoveFor, mousePosition);
                    _graphView.AddElement(cameraMoveForNode);
                    return true;
                case DialogueType.CameraTransition:
                    BaseNode cameraTransitionNode = _graphView.CreateNode(DialogueType.CameraTransition, mousePosition);
                    _graphView.AddElement(cameraTransitionNode);
                    return true;
                case DialogueType.CameraLookAt:
                    BaseNode cameraLookAtNode = _graphView.CreateNode(DialogueType.CameraLookAt, mousePosition);
                    _graphView.AddElement(cameraLookAtNode);
                    return true;
                case DialogueType.WaitForSeconds:
                    BaseNode waitForSecondsNode = _graphView.CreateNode(DialogueType.WaitForSeconds, mousePosition);
                    _graphView.AddElement(waitForSecondsNode);
                    return true;
                case Group _:
                    GraphElement group = _graphView.CreateGroup("New Group", true);
                    _graphView.AddElement(group);
                    return true;
            }

            return false;
        }
    }
}
