using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using ColbyO.SJ26.Street;
using PlazmaGames.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColbyO.SJ26.GPS
{
    public class GPSMonoSystem : MonoBehaviour, IGPSMonoSystem
    {
        [Header("Testing Configuration")]
        [SerializeField] private string _startAddress;
        [SerializeField] private string _targetAddress;

        private IStreetMonoSystem _streetSystem;
        private List<Vector3> _currentCalculatedPath = new List<Vector3>();

        private void Update()
        {
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
            if (_streetSystem == null) return;

            Vector3 startPos = GetAddressPosition(_startAddress);
            _currentCalculatedPath = CalculatePath(startPos, _targetAddress);
        }

        private AddressMarker FindAddressMarker(string addressSearch)
        {
            if (string.IsNullOrEmpty(addressSearch) || _streetSystem == null) return null;

            var streets = _streetSystem.GetNamedStreets();
            foreach (var street in streets)
            {
                foreach (var addr in street.addresses)
                {
                    if (addr.addressLabel.Equals(addressSearch, System.StringComparison.OrdinalIgnoreCase) ||
                        addr.addressLabel.Contains(addressSearch))
                    {
                        return addr;
                    }
                }
            }

            if (streets.Count > 0 && streets[0].addresses.Count > 0)
            {
                return streets[0].addresses[0];
            }
            return null;
        }

        private Vector3 GetAddressPosition(string addressSearch)
        {
            var marker = FindAddressMarker(addressSearch);
            return marker != null ? marker.position : Vector3.zero;
        }

        public List<Vector3> CalculatePath(Vector3 startPosition, string targetAddressSearch)
        {
            List<Vector3> pathPoints = new List<Vector3>();
            if (_streetSystem == null) return pathPoints;

            var nodes = _streetSystem.GetNodes();
            var edges = _streetSystem.GetEdges();
            if (nodes == null || nodes.Count == 0 || edges == null || edges.Count == 0) return pathPoints;

            AddressMarker targetMarker = FindAddressMarker(targetAddressSearch);
            if (targetMarker == null) return pathPoints;

            Vector3 targetPosition = targetMarker.position;

            StreetGraphEdge startEdge = FindClosestEdge(startPosition, edges, out float startEdgeT);
            StreetGraphEdge targetEdge = targetMarker.parentEdge ?? FindClosestEdge(targetPosition, edges, out _);
            float targetEdgeT = targetMarker.parentEdge != null ? targetMarker.tOnEdge : 0.5f;

            if (startEdge == null || targetEdge == null) return pathPoints;

            Transform containerTransform = _streetSystem.GetSplineContainerTransform();

            if (startEdge == targetEdge)
            {
                SampleEdgeSegment(startEdge, startEdgeT, targetEdgeT, containerTransform, pathPoints);
                return pathPoints;
            }

            StreetGraphNode[] startCandidates = { startEdge.startNode, startEdge.endNode };
            StreetGraphNode[] targetCandidates = { targetEdge.startNode, targetEdge.endNode };

            float bestTotalCost = float.MaxValue;
            List<(StreetGraphEdge edge, StreetGraphNode fromNode, StreetGraphNode toNode)> bestEdgeChain = null;
            StreetGraphNode chosenStartNode = null;
            StreetGraphNode chosenTargetNode = null;

            foreach (var sNode in startCandidates)
            {
                float distToSNode = (sNode == startEdge.startNode)
                    ? startEdge.length * startEdgeT
                    : startEdge.length * (1f - startEdgeT);

                foreach (var tNode in targetCandidates)
                {
                    float distFromTNode = (tNode == targetEdge.startNode)
                        ? targetEdge.length * targetEdgeT
                        : targetEdge.length * (1f - targetEdgeT);

                    var chain = FindShortestEdgePath(sNode, tNode, out float graphCost);
                    if (chain != null)
                    {
                        float totalCost = distToSNode + graphCost + distFromTNode;
                        if (totalCost < bestTotalCost)
                        {
                            bestTotalCost = totalCost;
                            bestEdgeChain = chain;
                            chosenStartNode = sNode;
                            chosenTargetNode = tNode;
                        }
                    }
                }
            }

            if (bestEdgeChain == null) return pathPoints;

            float startExitT = (chosenStartNode == startEdge.startNode) ? 0f : 1f;
            SampleEdgeSegment(startEdge, startEdgeT, startExitT, containerTransform, pathPoints);

            foreach (var step in bestEdgeChain)
            {
                bool forward = (step.fromNode == step.edge.startNode);
                SampleEdgeSegment(step.edge, forward ? 0f : 1f, forward ? 1f : 0f, containerTransform, pathPoints);
            }

            float targetEntryT = (chosenTargetNode == targetEdge.startNode) ? 0f : 1f;
            SampleEdgeSegment(targetEdge, targetEntryT, targetEdgeT, containerTransform, pathPoints);

            return pathPoints;
        }

        private void SampleEdgeSegment(StreetGraphEdge edge, float fromEdgeT, float toEdgeT, Transform containerTransform, List<Vector3> outPoints)
        {
            if (edge.splineSegment == null) return;

            int steps = 12;
            for (int i = 0; i <= steps; i++)
            {
                float lerp = i / (float)steps;
                float edgeT = Mathf.Lerp(fromEdgeT, toEdgeT, lerp);
                float splineT = Mathf.Lerp(edge.startT, edge.endT, edgeT);

                Vector3 localPos = (Vector3)edge.splineSegment.EvaluatePosition(splineT);
                Vector3 worldPos = containerTransform.TransformPoint(localPos);

                if (outPoints.Count == 0 || Vector3.Distance(outPoints[outPoints.Count - 1], worldPos) > 0.02f)
                {
                    outPoints.Add(worldPos);
                }
            }
        }

        private StreetGraphEdge FindClosestEdge(Vector3 worldPos, List<StreetGraphEdge> edges, out float bestEdgeT)
        {
            StreetGraphEdge bestEdge = null;
            float minDist = float.MaxValue;
            bestEdgeT = 0f;
            Transform containerTransform = _streetSystem.GetSplineContainerTransform();

            foreach (var edge in edges)
            {
                if (edge.splineSegment == null) continue;

                int samples = 8;
                for (int i = 0; i <= samples; i++)
                {
                    float eT = i / (float)samples;
                    float sT = Mathf.Lerp(edge.startT, edge.endT, eT);
                    Vector3 pt = containerTransform.TransformPoint((Vector3)edge.splineSegment.EvaluatePosition(sT));
                    float d = Vector3.Distance(worldPos, pt);
                    if (d < minDist)
                    {
                        minDist = d;
                        bestEdge = edge;
                        bestEdgeT = eT;
                    }
                }
            }
            return bestEdge;
        }

        private List<(StreetGraphEdge edge, StreetGraphNode fromNode, StreetGraphNode toNode)> FindShortestEdgePath(
            StreetGraphNode start, StreetGraphNode goal, out float totalPathCost)
        {
            var pathEdges = new List<(StreetGraphEdge edge, StreetGraphNode fromNode, StreetGraphNode toNode)>();
            totalPathCost = 0f;

            if (start == goal) return pathEdges;

            var gScore = new Dictionary<StreetGraphNode, float>();
            var cameFromEdge = new Dictionary<StreetGraphNode, StreetGraphEdge>();
            var cameFromNode = new Dictionary<StreetGraphNode, StreetGraphNode>();
            var openSet = new List<StreetGraphNode>();

            foreach (var node in _streetSystem.GetNodes())
            {
                gScore[node] = float.MaxValue;
            }

            gScore[start] = 0f;
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                StreetGraphNode current = openSet[0];
                float lowestScore = gScore[current];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (gScore[openSet[i]] < lowestScore)
                    {
                        current = openSet[i];
                        lowestScore = gScore[current];
                    }
                }

                if (current == goal)
                {
                    totalPathCost = gScore[goal];
                    StreetGraphNode currNode = goal;
                    while (currNode != start)
                    {
                        StreetGraphEdge edge = cameFromEdge[currNode];
                        StreetGraphNode prevNode = cameFromNode[currNode];
                        pathEdges.Add((edge, prevNode, currNode));
                        currNode = prevNode;
                    }
                    pathEdges.Reverse();
                    return pathEdges;
                }

                openSet.Remove(current);

                foreach (var edge in current.connectedEdges)
                {
                    StreetGraphNode neighbor = (edge.startNode == current) ? edge.endNode : edge.startNode;
                    if (neighbor == null) continue;

                    float tentativeGScore = gScore[current] + edge.length;
                    if (tentativeGScore < gScore[neighbor])
                    {
                        cameFromNode[neighbor] = current;
                        cameFromEdge[neighbor] = edge;
                        gScore[neighbor] = tentativeGScore;

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            totalPathCost = float.MaxValue;
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_currentCalculatedPath == null || _currentCalculatedPath.Count < 2) return;

            Gizmos.color = Color.red;
            for (int i = 0; i < _currentCalculatedPath.Count - 1; i++)
            {
                Vector3 p1 = _currentCalculatedPath[i];
                Vector3 p2 = _currentCalculatedPath[i + 1];
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p1 + Vector3.up * 0.15f, p2 + Vector3.up * 0.15f);
            }
        }
#endif
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GPSMonoSystem))]
    public class GPSMonoSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GPSMonoSystem gps = (GPSMonoSystem)target;

            List<string> addressOptions = new List<string> { "" };

            IStreetMonoSystem streetSys = GameManager.GetMonoSystem<IStreetMonoSystem>();

            if (streetSys != null && streetSys.GetNamedStreets() != null)
            {
                foreach (var street in streetSys.GetNamedStreets())
                {
                    foreach (var addr in street.addresses)
                    {
                        addressOptions.Add(addr.addressLabel);
                    }
                }
            }

            SerializedProperty startAddrProp = serializedObject.FindProperty("_startAddress");
            SerializedProperty targetAddrProp = serializedObject.FindProperty("_targetAddress");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Testing Configuration", EditorStyles.boldLabel);

            int startIndex = Mathf.Max(0, addressOptions.IndexOf(startAddrProp.stringValue));
            startIndex = EditorGUILayout.Popup("Start Address", startIndex, addressOptions.ToArray());
            if (startIndex >= 0 && startIndex < addressOptions.Count)
            {
                startAddrProp.stringValue = addressOptions[startIndex];
            }

            int targetIndex = Mathf.Max(0, addressOptions.IndexOf(targetAddrProp.stringValue));
            targetIndex = EditorGUILayout.Popup("Target Address", targetIndex, addressOptions.ToArray());
            if (targetIndex >= 0 && targetIndex < addressOptions.Count)
            {
                targetAddrProp.stringValue = addressOptions[targetIndex];
            }

            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
        }
    }
#endif
}