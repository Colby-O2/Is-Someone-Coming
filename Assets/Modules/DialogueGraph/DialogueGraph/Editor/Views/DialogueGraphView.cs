using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;
using UnityEngine.UIElements;

using UnityEditor.Experimental.GraphView;

using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using DialogueGraph.Editor.Nodes;

namespace DialogueGraph.Editor.Views
{
    internal sealed class DialogueGraphView : GraphView
    {
        public Blackboard Blackboard { get; set; }
        public List<ExposedProperty> ExposedProperties { get; private set; }

        private DialogueGraphEditorWindow _editorWindow;
        private NodeSearchWindow _searchWindow;
        private Vector2 _mousePosition;

        /// <summary>
        /// DialogueGraphView's constructor
        /// </summary>
        public DialogueGraphView(DialogueGraphEditorWindow editorWindow)
        {
            ExposedProperties = new List<ExposedProperty>();
            _editorWindow = editorWindow;
            CreateGrid();
            AddManipulator();
            AddStyleSheets();
            AddSearchWindow();
            RegisterCallback<KeyDownEvent>(evt =>
            {
                if ((evt.ctrlKey || evt.commandKey) && evt.keyCode == KeyCode.S)
                {
                    evt.StopPropagation();
                    _editorWindow.Save();
                }
            });
            AddElement(CreateNode(DialogueType.Start, Vector2.zero));
        }

        public void ClearGraph(bool replsaceStart = false)
        {
            DeleteElements(nodes);
            DeleteElements(edges);
            DeleteElements(graphElements.OfType<Group>());
            Blackboard.Clear();
            ExposedProperties.Clear();
            if (replsaceStart) AddElement(CreateNode(DialogueType.Start, Vector2.zero));
        }

        public Group GetNodeGroup(Node node)
        {
            return graphElements
                .OfType<Group>()
                .FirstOrDefault(g => g.ContainsElement(node));
        }

        private void AddSearchWindow()
        {
            if (!_searchWindow)
            {
                _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            }

            _searchWindow.Initialize(this);
            nodeCreationRequest = context =>
            {
                _mousePosition = context.screenMousePosition - _editorWindow.position.position;
                
                SearchWindow.Open(
                    new SearchWindowContext(context.screenMousePosition),
                    _searchWindow
                );
            };
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort == port) return;
                if (startPort.node == port.node) return;
                if (startPort.direction == port.direction) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        private IManipulator CreateGroupContextMenu()
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction("Add Group", e => AddElement(CreateGroup("New Group")))
            );

            return contextualMenuManipulator;
        }

        public GraphElement CreateGroup(string title, bool isSearchWindow = false)
        {
            Group group = new Group()
            {
                title = title
            };
            group.SetPosition(new Rect(isSearchWindow ? contentViewContainer.WorldToLocal(_mousePosition) : Vector2.zero, Vector2.zero));
            return group;
        }

        private IManipulator CreateNodeMenu(DialogueType type, string title)
        {
            ContextualMenuManipulator contextualMenuManipulator = new ContextualMenuManipulator(
                menuEvent => menuEvent.menu.AppendAction(title, e => AddElement(CreateNode(type, Vector2.zero)))
            );

            return contextualMenuManipulator;
        }

        private void AddManipulator()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            this.AddManipulator(CreateGroupContextMenu());
            this.AddManipulator(CreateNodeMenu(DialogueType.SingleChoice, "Add Node/Single Choice"));
            this.AddManipulator(CreateNodeMenu(DialogueType.MultipleChoice, "Add Node/Multiple Choice"));
            this.AddManipulator(CreateNodeMenu(DialogueType.SetBoolean, "Add Node/Set Boolean"));
            this.AddManipulator(CreateNodeMenu(DialogueType.Branch, "Add Node/Branch"));
            this.AddManipulator(CreateNodeMenu(DialogueType.SetInt, "Add Node/Set Int"));
            this.AddManipulator(CreateNodeMenu(DialogueType.Increment, "Add Node/Increment"));
            this.AddManipulator(CreateNodeMenu(DialogueType.Comparator, "Add Node/Comparator"));
            this.AddManipulator(CreateNodeMenu(DialogueType.WaitForSeconds, "Add Node/Wait For Seconds"));
            this.AddManipulator(CreateNodeMenu(DialogueType.Connector, "Add Node/Connector"));
            this.AddManipulator(CreateNodeMenu(DialogueType.EnableCinematicCamera, "Add Node/Enable Cinematic Camera"));
            this.AddManipulator(CreateNodeMenu(DialogueType.DisableCinematicCamera, "Add Node/Diable Cinematic Camera"));
            this.AddManipulator(CreateNodeMenu(DialogueType.CameraMove, "Add Node/Camera Move"));
            this.AddManipulator(CreateNodeMenu(DialogueType.CameraMoveFor, "Add Node/Camera Move For"));
            this.AddManipulator(CreateNodeMenu(DialogueType.CameraTransition, "Add Node/Camera Transition"));
            this.AddManipulator(CreateNodeMenu(DialogueType.CameraLookAt, "Add Node/Camera Look At"));
            //this.AddManipulator(CreateNodeMenu(DialogueType.Start, "Add Node/Start"));

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        }

 
        private void CreateGrid()
        {
            GridBackground gridBackground = new GridBackground();
            gridBackground.StretchToParentSize();
            Insert(0, gridBackground);
        }

        private void AddStyleSheets()
        {
            this.AddStyleSheets(
                "GraphViewStyles",
                "NodeStyle",
                "DialogueGraphVariables"
            );
        }

        public Vector2 GetLocalMousePosition(Vector2 pos, bool isSearchWindow = false)
        {
            // Places Search windows at mouse location for Event.current but places from context menu near origin since conversion with .WorldToLocal won't work
            if (isSearchWindow) return contentViewContainer.WorldToLocal(_mousePosition);
            else return pos;
        }

        public BaseNode CreateNode(DialogueType type)
        {
            Type nodeType = Type.GetType($"DialogueGraph.Editor.Nodes.{type}Node");
            return Activator.CreateInstance(nodeType) as BaseNode;
        }

        public BaseNode CreateNode(DialogueType type, Vector2 pos)
        {
            BaseNode node = CreateNode(type);
            node.Initialize(pos, this);
            node.Draw();
            return node;
        }

        public void OnPropertyChanged(string oldValue = null, string newValue = null)
        {
            foreach (Node node in nodes)
            {
                if (node is SetBooleanNode setBooleanNode)
                {
                    setBooleanNode.UpdateVariables(ExposedProperties.Where(e => e.Type == "Bool").Select(e => e.Name).ToList(), oldValue, newValue);
                }
                else if (node is BranchNode comparatorNode)
                {
                    comparatorNode.UpdateVariables(ExposedProperties.Where(e => e.Type == "Bool").Select(e => e.Name).ToList(), oldValue, newValue);
                }
                else if (node is IncrementNode incrementNode)
                {
                    incrementNode.UpdateVariables(ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).ToList(), oldValue, newValue);
                }
                else if (node is ComparatorNode camparatorNode)
                {
                    camparatorNode.UpdateVariables(ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).ToList(), oldValue, newValue);
                }
                else if (node is SetIntNode setIntNode)
                {
                    setIntNode.UpdateVariables(ExposedProperties.Where(e => e.Type == "Int").Select(e => e.Name).ToList(), oldValue, newValue);
                }
            }
        }

        public void RemovePropertyFromBlackboard(string name)
        {
            ExposedProperty property = ExposedProperties.FirstOrDefault(e => e.Name == name);

            if (property != null)
            {
                ExposedProperties.Remove(property);
            }

            OnPropertyChanged();
        }

        public void AddPropertyToBlackBoard(ExposedProperty exposedProperty)
        {
            string localName = exposedProperty.Name;
            object localValue = exposedProperty.Value;
            string localType = exposedProperty.Type;

            while (ExposedProperties.Any(e => e.Name == localName)) localName = $"{localName}(1)";

            ExposedProperty property = new ExposedProperty
            {
                Name = localName,
                Type = localType,
                Value = localValue
            };

            BlackboardField blackboardField = new BlackboardField()
            {
                text = property.Name,
                typeText = property.Type,
                userData = property
            };

            VisualElement propertyEditor = new VisualElement();

            if (property.Type == "Bool")
            {
                var toggle = new Toggle("Value")
                {
                    value = property.Value is bool b && b
                };
                toggle.RegisterValueChangedCallback(evt =>
                {
                    int propertyIndex = ExposedProperties.FindIndex(e => e.Name == property.Name);
                    ExposedProperties[propertyIndex].Value = evt.newValue;
                });
                propertyEditor.Add(toggle);
            }
            else if (property.Type == "Int")
            {
                var intField = new IntegerField("Value")
                {
                    value = property.Value is int i ? i : 0
                };
                intField.RegisterValueChangedCallback(evt =>
                {
                    int propertyIndex = ExposedProperties.FindIndex(e => e.Name == property.Name);
                    ExposedProperties[propertyIndex].Value = evt.newValue;
                });
                propertyEditor.Add(intField);
            }

            BlackboardRow row = new BlackboardRow(blackboardField, propertyEditor);

            property.Container = row;
            Blackboard.Add(row);
            ExposedProperties.Add(property);

            OnPropertyChanged();
        }
    }
}
