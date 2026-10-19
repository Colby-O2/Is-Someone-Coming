using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using PlazmaGames.Core.Debugging;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColbyO.SJ26.Street
{
    [System.Serializable]
    public class StreetGraphNode
    {
        public Vector3 position;
        public List<StreetGraphEdge> connectedEdges = new List<StreetGraphEdge>();
        public string intersectionName;
    }

    [System.Serializable]
    public class StreetGraphEdge
    {
        public int splineIndex;
        public StreetGraphNode startNode;
        public StreetGraphNode endNode;
        public float startT;
        public float endT;
        public float length;
        public Spline splineSegment;
        public NamedStreet parentStreet;
    }

    [System.Serializable]
    public class AddressMarker
    {
        public Vector3 position;
        public string addressLabel;
        public StreetGraphEdge parentEdge;
        public float tOnEdge;
    }

    [System.Serializable]
    public class NamedStreet
    {
        public string streetName;
        public List<StreetGraphEdge> edges = new List<StreetGraphEdge>();
        public List<AddressMarker> addresses = new List<AddressMarker>();
        public float totalLength;
        public Color displayColor;
    }

    public class StreetMonoSystem : MonoBehaviour, IStreetMonoSystem
    {
        [Header("Configuration")]
        [SerializeField] private float _vertexMergeTolerance = 2.0f;

        [Header("Address Generation")]
        [SerializeField] private float _addressIntervalMeters = 25f;
        [SerializeField] private float _intersectionBufferMeters = 8f;
        [SerializeField] private int _startingHouseNumber = 100;

        [Header("Procedural Name Pools")]
        [SerializeField] private List<string> _streetRoots = new List<string>
        {
            "Maple", "Pine", "Oak", "Cedar", "Hill", "Erie", "Elm", "Walnut",
            "Chestnut", "Washington", "River", "Lake", "Mill", "Main", "Church",
            "Park", "Sunset", "Ridge", "Cemetery", "Ash", "Locust", "Sycamore"
        };

        [SerializeField] private List<string> _streetSuffixes = new List<string>
        {
            "St", "Ave", "Rd", "Blvd", "Ln", "Ct", "Dr", "Way"
        };

        [Header("Debug Visualization")]
        [SerializeField] private bool _showRoadLines = true;
        [SerializeField] private bool _showAddresses = true;
        [SerializeField] private bool _showIntersections = true;

        private List<StreetGraphNode> _nodes = new List<StreetGraphNode>();
        private List<StreetGraphEdge> _edges = new List<StreetGraphEdge>();
        private List<NamedStreet> _namedStreets = new List<NamedStreet>();

        private SplineContainer _splineContainer;

        public List<StreetGraphNode> GetNodes() => _nodes;
        public List<StreetGraphEdge> GetEdges() => _edges;
        public List<NamedStreet> GetNamedStreets() => _namedStreets;
        public Transform GetSplineContainerTransform() => _splineContainer != null ? _splineContainer.transform : transform;

        private void Start()
        {
            GameObject splineGO = GameObject.FindWithTag("StreetSpline");
            if (splineGO != null)
            {
                _splineContainer = splineGO.GetComponent<SplineContainer>();
            }

            if (_splineContainer != null)
            {
                ParseSplinesIntoGraph();
                GroupIntoStreets();
                NameIntersections();
                AssignAddresses();
            }
            else
            {
                PlazmaDebug.LogError("No SplineContainer found or tagged with 'StreetSpline'!", "StreetMonoSystem");
            }
        }

        private void ParseSplinesIntoGraph()
        {
            _nodes.Clear();
            _edges.Clear();

            var splines = _splineContainer.Splines;
            Transform containerTrans = _splineContainer.transform;

            List<List<float>> splineSplitT = new List<List<float>>();

            for (int i = 0; i < splines.Count; i++)
            {
                Spline s = splines[i];
                List<float> splits = new List<float> { 0f, 1f };

                for (int k = 1; k < s.Count - 1; k++)
                {
                    float t = s.ConvertIndexUnit(k, PathIndexUnit.Knot, PathIndexUnit.Normalized);
                    AddSplitT(splits, t);
                }

                splineSplitT.Add(splits);
            }

            for (int i = 0; i < splines.Count; i++)
            {
                Spline splineA = splines[i];
                if (splineA.Count < 2) continue;

                for (int k = 0; k < splineA.Count; k++)
                {
                    float3 localKnot = splineA[k].Position;
                    Vector3 worldKnot = containerTrans.TransformPoint((Vector3)localKnot);

                    for (int j = 0; j < splines.Count; j++)
                    {
                        if (i == j) continue;
                        Spline splineB = splines[j];
                        if (splineB.Count < 2) continue;

                        float3 localToB = (float3)containerTrans.InverseTransformPoint(worldKnot);
                        SplineUtility.GetNearestPoint(splineB, localToB, out float3 nearestLocalB, out float tB);
                        Vector3 nearestWorldB = containerTrans.TransformPoint((Vector3)nearestLocalB);

                        if (Vector3.Distance(worldKnot, nearestWorldB) <= _vertexMergeTolerance)
                        {
                            AddSplitT(splineSplitT[j], tB);
                        }
                    }
                }
            }

            for (int i = 0; i < splines.Count; i++)
            {
                Spline splineA = splines[i];
                if (splineA.Count < 2) continue;

                int samplesA = Mathf.Max(10, Mathf.CeilToInt(splineA.GetLength() / 2f));
                for (int s = 0; s <= samplesA; s++)
                {
                    float tA = s / (float)samplesA;
                    Vector3 worldPosA = containerTrans.TransformPoint((Vector3)splineA.EvaluatePosition(tA));

                    for (int j = i + 1; j < splines.Count; j++)
                    {
                        Spline splineB = splines[j];
                        if (splineB.Count < 2) continue;

                        float3 localToB = (float3)containerTrans.InverseTransformPoint(worldPosA);
                        SplineUtility.GetNearestPoint(splineB, localToB, out float3 nearestLocalB, out float tB);
                        Vector3 nearestWorldB = containerTrans.TransformPoint((Vector3)nearestLocalB);

                        if (Vector3.Distance(worldPosA, nearestWorldB) <= _vertexMergeTolerance)
                        {
                            AddSplitT(splineSplitT[i], tA);
                            AddSplitT(splineSplitT[j], tB);
                        }
                    }
                }
            }

            for (int i = 0; i < splines.Count; i++)
            {
                Spline spline = splines[i];
                if (spline.Count < 2) continue;

                float totalLength = spline.GetLength();
                List<float> splits = splineSplitT[i];
                splits.Sort();

                for (int s = 0; s < splits.Count - 1; s++)
                {
                    float t0 = splits[s];
                    float t1 = splits[s + 1];
                    if (t1 - t0 < 0.005f) continue;

                    Vector3 startPos = containerTrans.TransformPoint((Vector3)spline.EvaluatePosition(t0));
                    Vector3 endPos = containerTrans.TransformPoint((Vector3)spline.EvaluatePosition(t1));

                    StreetGraphNode startNode = GetOrCreateNode(startPos);
                    StreetGraphNode endNode = GetOrCreateNode(endPos);

                    if (startNode == endNode) continue;

                    StreetGraphEdge edge = new StreetGraphEdge
                    {
                        splineIndex = i,
                        startNode = startNode,
                        endNode = endNode,
                        startT = t0,
                        endT = t1,
                        length = totalLength * (t1 - t0),
                        splineSegment = spline
                    };

                    startNode.connectedEdges.Add(edge);
                    endNode.connectedEdges.Add(edge);
                    _edges.Add(edge);
                }
            }

            PlazmaDebug.Log($"Graph parsed: {_nodes.Count} _nodes, {_edges.Count} connected road segments.", "StreetSystem");
        }

        private void AddSplitT(List<float> list, float t)
        {
            t = Mathf.Clamp01(t);
            foreach (float existing in list)
            {
                if (Mathf.Abs(existing - t) < 0.02f) return;
            }
            list.Add(t);
        }

        private StreetGraphNode GetOrCreateNode(Vector3 pos)
        {
            foreach (var node in _nodes)
            {
                if (Vector3.Distance(node.position, pos) <= _vertexMergeTolerance)
                {
                    return node;
                }
            }

            var newNode = new StreetGraphNode { position = pos };
            _nodes.Add(newNode);
            return newNode;
        }

        private void GroupIntoStreets()
        {
            _namedStreets.Clear();
            HashSet<string> usedNames = new HashSet<string>();
            int rootIndex = 0;
            int suffixIndex = 0;
            int numericFallbackCounter = 1;

            Dictionary<int, List<StreetGraphEdge>> edgesBySpline = new Dictionary<int, List<StreetGraphEdge>>();
            foreach (var edge in _edges)
            {
                if (!edgesBySpline.ContainsKey(edge.splineIndex))
                {
                    edgesBySpline[edge.splineIndex] = new List<StreetGraphEdge>();
                }
                edgesBySpline[edge.splineIndex].Add(edge);
            }

            foreach (var kvp in edgesBySpline)
            {
                NamedStreet street = new NamedStreet();
                bool nameAssigned = false;
                int safetyBreak = 0;

                while (!nameAssigned && safetyBreak < 200)
                {
                    safetyBreak++;
                    string root = _streetRoots[rootIndex % _streetRoots.Count];
                    string suffix = _streetSuffixes[suffixIndex % _streetSuffixes.Count];

                    rootIndex++;
                    if (rootIndex % _streetRoots.Count == 0)
                    {
                        suffixIndex++;
                    }

                    string candidateName = $"{root} {suffix}";
                    if (!usedNames.Contains(candidateName))
                    {
                        usedNames.Add(candidateName);
                        street.streetName = candidateName;
                        nameAssigned = true;
                    }
                }

                if (!nameAssigned)
                {
                    street.streetName = $"Sector {numericFallbackCounter++} St";
                }

                float hue = (_namedStreets.Count * 0.37f) % 1.0f;
                street.displayColor = Color.HSVToRGB(hue, 0.85f, 0.95f);

                foreach (var edge in kvp.Value)
                {
                    edge.parentStreet = street;
                    street.edges.Add(edge);
                    street.totalLength += edge.length;
                }

                _namedStreets.Add(street);
            }

            PlazmaDebug.Log($"Grouped road network into {_namedStreets.Count} unique named streets.", "StreetSystem");
        }

        private void NameIntersections()
        {
            foreach (var node in _nodes)
            {
                List<string> uniqueStreets = new List<string>();

                foreach (var edge in node.connectedEdges)
                {
                    if (edge.parentStreet != null && !string.IsNullOrEmpty(edge.parentStreet.streetName))
                    {
                        if (!uniqueStreets.Contains(edge.parentStreet.streetName))
                        {
                            uniqueStreets.Add(edge.parentStreet.streetName);
                        }
                    }
                }

                if (uniqueStreets.Count >= 2)
                {
                    node.intersectionName = $"{uniqueStreets[0]} At {uniqueStreets[1]}";
                }
                else
                {
                    node.intersectionName = string.Empty;
                }
            }
        }

        private void AssignAddresses()
        {
            foreach (var street in _namedStreets)
            {
                street.addresses.Clear();
                int currentHouseNumber = _startingHouseNumber;
                float distanceRemainder = _addressIntervalMeters * 0.5f;

                foreach (var edge in street.edges)
                {
                    float edgeLength = edge.length;
                    float currentPosOnEdge = distanceRemainder;

                    while (currentPosOnEdge < edgeLength)
                    {
                        float edgeT = edgeLength > 0f ? (currentPosOnEdge / edgeLength) : 0f;
                        float splineT = Mathf.Lerp(edge.startT, edge.endT, edgeT);

                        Vector3 localPos = (Vector3)edge.splineSegment.EvaluatePosition(splineT);
                        Vector3 worldPos = _splineContainer.transform.TransformPoint(localPos);

                        if (!IsTooCloseToIntersection(worldPos))
                        {
                            street.addresses.Add(new AddressMarker
                            {
                                position = worldPos,
                                addressLabel = $"{currentHouseNumber} {street.streetName}",
                                parentEdge = edge,
                                tOnEdge = edgeT
                            });

                            currentHouseNumber += 2;
                        }

                        currentPosOnEdge += _addressIntervalMeters;
                    }

                    distanceRemainder = currentPosOnEdge - edgeLength;
                }
            }
        }

        private bool IsTooCloseToIntersection(Vector3 pos)
        {
            foreach (var node in _nodes)
            {
                if (!string.IsNullOrEmpty(node.intersectionName) || node.connectedEdges.Count >= 3)
                {
                    if (Vector3.Distance(pos, node.position) < _intersectionBufferMeters)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_namedStreets != null && _namedStreets.Count > 0)
            {
                foreach (var street in _namedStreets)
                {
                    Gizmos.color = street.displayColor;

                    if (_showRoadLines)
                    {
                        foreach (var edge in street.edges)
                        {
                            if (edge.splineSegment == null || _splineContainer == null) continue;

                            int resolution = 10;
                            float startT = edge.startT;
                            float endT = edge.endT;

                            Vector3 prevPoint = _splineContainer.transform.TransformPoint((Vector3)edge.splineSegment.EvaluatePosition(startT));

                            for (int i = 1; i <= resolution; i++)
                            {
                                float t = Mathf.Lerp(startT, endT, i / (float)resolution);
                                Vector3 nextPoint = _splineContainer.transform.TransformPoint((Vector3)edge.splineSegment.EvaluatePosition(t));

                                Gizmos.DrawLine(prevPoint, nextPoint);
                                prevPoint = nextPoint;
                            }
                        }
                    }

                    if (_showAddresses)
                    {
                        foreach (var addr in street.addresses)
                        {
                            Gizmos.DrawWireSphere(addr.position, 0.6f);
                            Handles.Label(addr.position + Vector3.up * 1.0f, addr.addressLabel);
                        }
                    }
                }
            }

            if (_showIntersections && _nodes != null)
            {
                Gizmos.color = Color.white;
                foreach (var node in _nodes)
                {
                    if (!string.IsNullOrEmpty(node.intersectionName))
                    {
                        Gizmos.DrawSphere(node.position, 0.8f);
                        Handles.Label(node.position + Vector3.up * 1.5f, node.intersectionName);
                    }
                }
            }
        }
#endif
    }
}