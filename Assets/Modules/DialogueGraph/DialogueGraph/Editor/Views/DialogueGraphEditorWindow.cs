using DialogueGraph.Data;
using DialogueGraph.Editor.Nodes;
using DialogueGraph.Enumeration;
using DialogueGraph.Utilities;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShortcutManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace DialogueGraph.Editor.Views
{
    internal sealed class DialogueGraphEditorWindow : EditorWindow
    {
        private const string DefaultWindowTitle = "Dialogue Graph";

        private DialogueGraphView _graphView;
        private Button _saveButton;
        private Button _loadButton;
        private Button _clearButton;
        private TextField _nameTextField;

        private void CreateGraphView()
        {
            _graphView = new DialogueGraphView(this);
            _graphView.StretchToParentSize();
            rootVisualElement.Add(_graphView);
        }

        public void Save()
        {
            DialogueGraphSO data = ScriptableObject.CreateInstance<DialogueGraphSO>();
            SaveGraph(data);
            AssetDatabase.CreateAsset(data, $"Assets/{_nameTextField.value}.asset");
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;
        }

        private void CreateToolbar()
        {
            
            Toolbar tb = new Toolbar();
            _nameTextField = EditorElementHelper.CreateTextField("New Dialogue", "Filename:");
            _saveButton = EditorElementHelper.CreateButton("Save", () =>
            {
                Save();
            });
            _loadButton = EditorElementHelper.CreateButton("Load", () =>
            {
                string path = EditorUtility.OpenFilePanel("Select Dialogue Graph", "Assets", "asset");
                if (!string.IsNullOrEmpty(path))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    DialogueGraphSO loadedData = AssetDatabase.LoadAssetAtPath<DialogueGraphSO>(relativePath);
                    if (loadedData != null)
                    {
                        LoadData(loadedData);  
                        _nameTextField.value = loadedData.name; 
                    }
                    else
                    {
                        Debug.LogWarning("Selected file is not a DialogueGraphSO");
                    }
                }
            });
            _clearButton = EditorElementHelper.CreateButton("Clear", () =>
            {
                _graphView.ClearGraph(true);
            });

            tb.Add(_nameTextField);
            tb.Add(_saveButton);
            tb.Add(_loadButton);
            tb.Add(_clearButton);
            tb.AddStyleSheets("ToolBarStyle");
            rootVisualElement.Add(tb);
        }

        private void OnEnable()
        {
            CreateGraphView();
            CreateToolbar();
            CreateMinimap();
            CreateBlackboard();
        }

        private void CreateBlackboard()
        {
            Blackboard bb = new Blackboard(_graphView);
            BlackboardSection bbSection = new BlackboardSection()
            {
                title = "Exposed Properties"
            };
            bb.Add(bbSection);

            bb.addItemRequested = blackboard =>
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Boolean"), false, () =>
                {
                    var property = new ExposedProperty()
                    {
                        Name = "New Boolean",
                        Type = "Bool",
                        Value = false
                    };
                    _graphView.AddPropertyToBlackBoard(property);
                });
                menu.AddItem(new GUIContent("Integer"), false, () =>
                {
                    var property = new ExposedProperty()
                    {
                        Name = "New Integer",
                        Type = "Int",
                        Value = 0
                    };
                    _graphView.AddPropertyToBlackBoard(property);
                });
                menu.ShowAsContext();
            };

            bb.editTextRequested = (blackboard, element, newValue) =>
            {
                string oldValue = ((BlackboardField)element).text;
                if (_graphView.ExposedProperties.Any(e => e.Name == newValue)) return;

                int propertyIndex = _graphView.ExposedProperties.FindIndex(e => e.Name == oldValue);
                _graphView.ExposedProperties[propertyIndex].Name = newValue;
                ((BlackboardField)element).text = newValue;
                _graphView.OnPropertyChanged(oldValue, newValue);
            };

            bb.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Delete)
                {
                    evt.StopPropagation();

                    List<ISelectable> selectables = bb.selection.ToList();
                    foreach (ISelectable selected in selectables)
                    {
                        if (selected is BlackboardField bbField && bbField.userData is ExposedProperty p)
                        {
                            _graphView.RemovePropertyFromBlackboard(p.Name);
                            if (p.Container != null && p.Container.parent != null) bb.Remove(p.Container);
                        }
                    }

                    bb.ClearSelection();
                }
            });

            bb.SetPosition(new Rect(10, 30, 200, 300));
            _graphView.Add(bb);
            _graphView.Blackboard = bb;
        }

        private void CreateMinimap()
        {
            MiniMap miniMap = new MiniMap()
            {
                anchored = true,
            };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            _graphView.Add(miniMap);
        }

        [MenuItem("Tools/Dialogue Graph")]
        public static void OpenWindow()
        {
            DialogueGraphEditorWindow wnd = GetWindow<DialogueGraphEditorWindow>();
            wnd.titleContent = new GUIContent(DefaultWindowTitle);
        }

        public void LoadData(DialogueGraphSO graphSO)
        {
            _graphView.ClearGraph();
            _nameTextField.value = AssetDatabase.GetAssetPath(graphSO).Substring("Assets/".Length).Replace(".asset", "");

            Dictionary<string, BaseNode> nodeMap = new Dictionary<string, BaseNode>();
            Dictionary<string, Group> groupMap = new Dictionary<string, Group>();

            foreach (DialogueExposedPropertiesData propertyData in graphSO.Properties)
            {
                ExposedProperty property = new ExposedProperty();
                property.Name = propertyData.Name;
                property.Type = propertyData.Type;
                property.Value = (propertyData.Type == "Bool") ? propertyData.BooleanValue : propertyData.IntValue;
                _graphView.AddPropertyToBlackBoard(property);
            }

            foreach (DialogueGroupData groupData in graphSO.Groups)
            {
                if (!groupMap.ContainsKey(groupData.Name))
                {
                    Group group = new Group()
                    { 
                        title = groupData.Name,
                    };
                    group.SetPosition(new Rect(groupData.Position, Vector2.zero));
                    _graphView.AddElement(group);
                    groupMap.Add(groupData.Name, group);
                }
            }

            if (
                !graphSO.Nodes.Any(
                    nodeData => nodeData.Type == DialogueType.Start
                ))
            {
                BaseNode node = _graphView.CreateNode(DialogueType.Start);

                node.Initialize(Vector2.zero, _graphView, node.ID);
                node.Draw();
                _graphView.AddElement(node);

                nodeMap.Add(node.ID, node);
            }

            foreach (DialogueNodeData nodeData in graphSO.Nodes)
            {
                BaseNode node = _graphView.CreateNode(nodeData.Type);
                node.ID = nodeData.Guid;
                node.Type = nodeData.Type;

                if (nodeData.GroupName != string.Empty &&  nodeData.GroupName != null)
                {
                    if (groupMap.ContainsKey(nodeData.GroupName))
                    {
                        groupMap[nodeData.GroupName].AddElement(node);
                    }
                }

                if (node is DialogueNode dialogueNode)
                {
                    dialogueNode.ActorName = nodeData.ActorName;
                    dialogueNode.Text = nodeData.Text;
                    dialogueNode.EventID = nodeData.EventID;
                    dialogueNode.Choices = nodeData.Choices.Select(e => e.Text).ToList();
                }
                else if (node is SetBooleanNode setBooleanNode)
                {
                    setBooleanNode.SelectedName = nodeData.PropertyName;
                    setBooleanNode.Value = nodeData.PropertyBooleanValue;
                }
                else if (node is BranchNode branchNode)
                {
                    branchNode.SelectedName = nodeData.PropertyName;
                }
                else if (node is IncrementNode incrementNode)
                {
                    incrementNode.SelectedName = nodeData.PropertyName;
                    incrementNode.Amount = nodeData.IncrementAmount;
                }
                else if (node is ComparatorNode comparatorNode)
                {
                    comparatorNode.SelectedName= nodeData.PropertyName;
                    comparatorNode.ComparisonType = nodeData.ComparisonType;
                    comparatorNode.Condition = nodeData.ComparsionCondition;
                }
                else if (node is SetIntNode setIntNode)
                {
                    setIntNode.SelectedName = nodeData.PropertyName;
                    setIntNode.Value = nodeData.PropertyIntValue;
                }
                else if (node is WaitForSecondsNode waitForSeconds)
                {
                    waitForSeconds.Duration = nodeData.Duration;
                }
                else if (node is EmitEventNode emitEventNode)
                {
                    emitEventNode.EventID = nodeData.EventID;
                }
                else if (node is CameraMoveNode cameraMoveNode)
                {
                    cameraMoveNode.LocationTag = nodeData.CameraToLocationTag;
                    cameraMoveNode.LookAtTag = nodeData.CameraLookAtTag;
                }
                else if (node is CameraMoveForNode cameraMoveForNode)
                {
                    cameraMoveForNode.LocationTag = nodeData.CameraToLocationTag;
                    cameraMoveForNode.LookAtTag = nodeData.CameraLookAtTag;
                    cameraMoveForNode.Duration = nodeData.Duration;
                }
                else if (node is CameraTransitionNode cameraTransitionNode)
                {
                    cameraTransitionNode.FromLocationTag = nodeData.CameraFromLocationTag;
                    cameraTransitionNode.ToLocationTag = nodeData.CameraToLocationTag;
                    cameraTransitionNode.LookAtTag = nodeData.CameraLookAtTag;
                    cameraTransitionNode.Duration = nodeData.Duration;
                }
                else if (node is CameraLookAtNode cameraLookAtNode)
                {
                    cameraLookAtNode.LookAtTag = nodeData.CameraLookAtTag;
                    cameraLookAtNode.Duration = nodeData.Duration;
                }

                node.Initialize(nodeData.position, _graphView, nodeData.Guid);
                node.Draw();
                _graphView.AddElement(node);

                nodeMap.Add(node.ID, node);
            }

            foreach (DialogueNodeData nodeData in graphSO.Nodes)
            {
                BaseNode node = nodeMap[nodeData.Guid];
                List<string> ids = nodeData.Choices.Select(e => e.Next).ToList();
                List<Port> ports = node.Query<Port>().Where(p => p.direction == Direction.Output).ToList();
                for (int i = 0; i < ids.Count; i++)
                {
                    if (ids[i] == string.Empty || ids[i] == null) continue;
                    BaseNode nextNode = nodeMap[ids[i]];
                    Port inPort = nextNode.Query<Port>().ToList().FirstOrDefault(p => p.direction == Direction.Input);
                    Edge edge = new Edge()
                    {
                        input  = inPort,
                        output = ports[i]
                    };
                    edge.input.Connect(edge);
                    edge.output.Connect(edge);
                    _graphView.AddElement(edge);
                }
            }
        }

        public void SaveGraph(DialogueGraphSO graphSO)
        {
            graphSO.Name = Path.GetFileName(_nameTextField.value);
            graphSO.Nodes.Clear();
            graphSO.Groups.Clear();
            graphSO.Properties.Clear();

            foreach(ExposedProperty property in _graphView.ExposedProperties)
            {
                DialogueExposedPropertiesData dialogueExposedPropertiesData = new DialogueExposedPropertiesData();
                dialogueExposedPropertiesData.Name = property.Name;
                dialogueExposedPropertiesData.Type = property.Type;
                if (property.Type == "Bool") dialogueExposedPropertiesData.BooleanValue = (bool)property.Value;
                else dialogueExposedPropertiesData.IntValue = (int)property.Value;
                graphSO.Properties.Add(dialogueExposedPropertiesData);
            }

            Node start = _graphView.nodes.ToList().FirstOrDefault(node => node is StartNode);

            foreach (Group group in _graphView.graphElements.OfType<Group>())
            {
                DialogueGroupData groupData = new DialogueGroupData();
                groupData.Name = group.title;
                groupData.Position = group.GetPosition().position;
                graphSO.Groups.Add(groupData);
            }

            foreach (Node node in _graphView.nodes.ToList())
            {
                if (node is BaseNode baseNode)
                {
                    DialogueNodeData data = new DialogueNodeData();
                    data.Guid = baseNode.ID;
                    data.Type = baseNode.Type;
                    data.position = node.GetPosition().position;

                    Group group = _graphView.GetNodeGroup(node);
                    if (group != null) data.GroupName = group.title;
                    else data.GroupName = string.Empty;

                    if (node is DialogueNode dialogueNode)
                    {
                        data.ActorName = dialogueNode.ActorName;
                        data.Text = dialogueNode.Text;
                        data.EventID = dialogueNode.EventID;
                    }
                    else if (node is SetBooleanNode setBooleanNode)
                    {
                        data.PropertyType = "Bool";
                        data.PropertyBooleanValue = setBooleanNode.Value;
                        data.PropertyName = setBooleanNode.SelectedName;
                    }
                    else if (node is BranchNode branchNode)
                    {
                        data.PropertyName = branchNode.SelectedName;
                    }
                    else if (node is IncrementNode incrementNode)
                    {
                        data.PropertyType = "Int";
                        data.PropertyName = incrementNode.SelectedName;
                        data.IncrementAmount = incrementNode.Amount;
                    }
                    else if (node is ComparatorNode comparatorNode)
                    {
                        data.PropertyType = "Int";
                        data.PropertyName = comparatorNode.SelectedName;
                        data.ComparisonType = comparatorNode.ComparisonType;
                        data.ComparsionCondition = comparatorNode.Condition;
                    }
                    else if (node is SetIntNode setIntNode)
                    {
                        data.PropertyType = "Int";
                        data.PropertyName = setIntNode.SelectedName;
                        data.PropertyIntValue = setIntNode.Value;
                    }
                    else if (node is WaitForSecondsNode waitForSeconds)
                    {
                        data.Duration = waitForSeconds.Duration;
                    }
                    else if (node is EmitEventNode emitEventNode)
                    {
                        data.EventID = emitEventNode.EventID;
                    }
                    else if (node is CameraMoveNode cameraMoveNode)
                    {
                        data.CameraToLocationTag = cameraMoveNode.LocationTag;
                        data.CameraLookAtTag = cameraMoveNode.LookAtTag;
                    }
                    else if (node is CameraMoveForNode cameraMoveForNode)
                    {
                        data.CameraToLocationTag = cameraMoveForNode.LocationTag;
                        data.CameraLookAtTag = cameraMoveForNode.LookAtTag;
                        data.Duration = cameraMoveForNode.Duration;
                    }
                    else if (node is CameraTransitionNode cameraTransitionNode)
                    {
                        data.CameraFromLocationTag = cameraTransitionNode.FromLocationTag;
                        data.CameraToLocationTag = cameraTransitionNode.ToLocationTag;
                        data.CameraLookAtTag = cameraTransitionNode.LookAtTag;
                        data.Duration = cameraTransitionNode.Duration;
                    }
                    else if (node is CameraLookAtNode cameraLookAtNode)
                    {
                        data.CameraLookAtTag = cameraLookAtNode.LookAtTag;
                        data.Duration = cameraLookAtNode.Duration;
                    }

                    data.Choices = new List<DialogueChoiceData>();
                    foreach (VisualElement port in baseNode.outputContainer.Children())
                    {
                        if (port is Port p)
                        {
                            DialogueChoiceData choiceData = new DialogueChoiceData();
                            TextField field = port.contentContainer.Q<TextField>();

                            if (field != null) choiceData.Text = field.value;
                            else choiceData.Text = string.Empty;

                            if (p.connected)
                            {
                                BaseNode nextNode = p.connections.First().input.node as BaseNode;
                                if (nextNode != null) choiceData.Next = nextNode.ID;
                            }

                            data.Choices.Add(choiceData);
                        }
                    }

                    graphSO.Nodes.Add(data);
                }
            }

            EditorUtility.SetDirty(graphSO);
        }
    }
}
