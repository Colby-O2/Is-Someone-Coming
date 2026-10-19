using UnityEngine;
using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Attribute;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Street;
using ColbyO.SJ26.GPS;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColbyO.SJ26.Dispatch
{
    public enum UnitType
    {
        Police,
        EMS,
        Fire
    }

    public enum UnitStatus
    {
        Available,
        EnRoute,
        OnScene,
        Unavailable
    }

    public class FieldUnit : MonoBehaviour
    {
        [Header("Unit Identity")]
        [SerializeField] private FieldUnitSO _unitData;
        [SerializeField, ReadOnly] private string _callsign = "P-01";
        [SerializeField, ReadOnly] private UnitStatus _status = UnitStatus.Available;

        [Header("Icon Display Settings")]
        [SerializeField] private GameObject _iconPrefab;
        [SerializeField] private Vector3 _iconOffset = new Vector3(0f, 2.5f, 0f);
        [SerializeField] private Vector2 _iconSize = new Vector2(9f * 0.8571f, 9f);
        [SerializeField] private bool _faceUpward = true;

        [Header("Route Renderer Settings")]
        [SerializeField] private Material _routeLineMaterial;
        [SerializeField] private Color _routeLineColor = new Color(0f, 0.75f, 1f, 1f);
        [SerializeField] private float _routeLineWidth = 1f;
        [SerializeField] private float _routeLineHeightOffset = 0.5f;

        [Header("Movement Settings")]
        [SerializeField] private float _waypointThreshold = 0.5f;
        [SerializeField] private bool _snapToRoadOnStart = true;
        [SerializeField] private string _startingAddress = "";

        [Header("Live Telemetry")]
        [SerializeField, ReadOnly] private string _activeCallId = "";
        [SerializeField, ReadOnly] private string _currentLocationLabel = "Patrolling";
        [SerializeField, ReadOnly] private string _destinationAddress = "None";

        private GameObject _spawnedIcon;
        private LineRenderer _routeLineRenderer;
        private float _idleTimer = 0f;
        private bool _isIdleCruising = false;

        private List<Vector3> _currentPath = new List<Vector3>();
        private int _pathIndex = 0;

        private IDispatchMonoSystem _dispatchSystem;
        private IStreetMonoSystem _streetSystem;
        private IGPSMonoSystem _gpsSystem;

        public string CallsignSymbol => _unitData.callsignSymbol;
        public string Callsign => _callsign;
        public UnitType Type => _unitData.unitType;
        public UnitStatus Status => _status;
        public string ActiveCallId => _activeCallId;
        public string CurrentLocation => _currentLocationLabel;
        public string Destination => _destinationAddress;
        public IReadOnlyList<Vector3> CurrentPath => _currentPath;
        public int CurrentPathIndex => _pathIndex;

        private void Start()
        {
            if (_iconPrefab == null)
            {
                _iconPrefab = Resources.Load<GameObject>("Prefabs/UnitIconPrefab");
            }

            _dispatchSystem = GameManager.GetMonoSystem<IDispatchMonoSystem>();
            _streetSystem = GameManager.GetMonoSystem<IStreetMonoSystem>();
            _gpsSystem = GameManager.GetMonoSystem<IGPSMonoSystem>();

            _dispatchSystem?.RegisterUnit(this);

            if (_snapToRoadOnStart && _streetSystem != null)
            {
                SnapToStartingRoadPosition();
            }

            SetupUnitIcon();
            SetupRouteRenderer();
            UpdateCurrentLocationLabel();
        }

        private void OnDestroy()
        {
            _dispatchSystem ??= GameManager.GetMonoSystem<IDispatchMonoSystem>();
            _dispatchSystem?.UnregisterUnit(this);
        }

        private void Update()
        {
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
            _gpsSystem ??= GameManager.GetMonoSystem<IGPSMonoSystem>();

            if (_status == UnitStatus.EnRoute || (_status == UnitStatus.Available && _isIdleCruising))
            {
                FollowPath();
                UpdateCurrentLocationLabel();
            }
            else if (_status == UnitStatus.Available)
            {
                HandleIdlePatrol();
            }

            UpdateRouteVisuals();
        }

        private void SetupUnitIcon()
        {
            if (_unitData == null) return;

            Texture targetTexture = _unitData.icon;

            if (targetTexture == null)
            {
                PlazmaDebug.LogWarning("No valid texture/sprite found on FieldUnitSO.", $"Unit {_callsign}");
                return;
            }

            if (_iconPrefab != null)
            {
                _spawnedIcon = Instantiate(_iconPrefab, transform);
            }

            if (_spawnedIcon == null) return;

            _spawnedIcon.transform.localPosition = _iconOffset;
            _spawnedIcon.transform.localScale = new Vector3(_iconSize.x, _iconSize.y, 1f);

            if (_faceUpward)
            {
                _spawnedIcon.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            if (_spawnedIcon.TryGetComponent<Renderer>(out var renderer))
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetTexture("_BaseMap", targetTexture);
                block.SetTexture("_MainTex", targetTexture);
                renderer.SetPropertyBlock(block);
            }
        }

        private void SetupRouteRenderer()
        {
            GameObject lineGO = new GameObject($"RouteLine_{_callsign}");
            lineGO.transform.SetParent(transform, false);
            lineGO.layer = _iconPrefab.layer;

            _routeLineRenderer = lineGO.AddComponent<LineRenderer>();
            _routeLineRenderer.useWorldSpace = true;
            _routeLineRenderer.alignment = LineAlignment.TransformZ;
            lineGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            _routeLineRenderer.startWidth = _routeLineWidth;
            _routeLineRenderer.endWidth = _routeLineWidth;
            _routeLineRenderer.numCapVertices = 4;
            _routeLineRenderer.numCornerVertices = 4;
            _routeLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _routeLineRenderer.receiveShadows = false;

            if (_routeLineMaterial != null)
            {
                _routeLineRenderer.material = _routeLineMaterial;
            }
            else
            {
                Shader defaultShader = Shader.Find("Sprites/Default");
                if (defaultShader != null)
                {
                    _routeLineRenderer.material = new Material(defaultShader);
                }
            }

            _routeLineRenderer.startColor = _routeLineColor;
            _routeLineRenderer.endColor = _routeLineColor;
            _routeLineRenderer.enabled = false;
        }

        private void UpdateRouteVisuals()
        {
            if (_routeLineRenderer == null) return;

            bool shouldDraw = (_status == UnitStatus.EnRoute && !_isIdleCruising && _currentPath != null && _pathIndex < _currentPath.Count);

            if (!shouldDraw)
            {
                if (_routeLineRenderer.enabled)
                {
                    _routeLineRenderer.positionCount = 0;
                    _routeLineRenderer.enabled = false;
                }
                return;
            }

            _routeLineRenderer.enabled = true;
            int remainingPoints = _currentPath.Count - _pathIndex;
            _routeLineRenderer.positionCount = remainingPoints + 1;

            Vector3 upOffset = Vector3.up * _routeLineHeightOffset;
            _routeLineRenderer.SetPosition(0, transform.position + upOffset);

            for (int i = 0; i < remainingPoints; i++)
            {
                _routeLineRenderer.SetPosition(i + 1, _currentPath[_pathIndex + i] + upOffset);
            }
        }

        private void HandleIdlePatrol()
        {
            if (_unitData == null || !_unitData.autoPatrolWhenAvailable || _unitData.unitType != UnitType.Police)
                return;

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= _unitData.idleWaitDuration)
            {
                _idleTimer = 0f;
                PickRandomPatrolDestination();
            }
        }

        private void PickRandomPatrolDestination()
        {
            var streets = _streetSystem?.GetNamedStreets();
            if (streets == null || streets.Count == 0) return;

            var candidateStreets = new List<NamedStreet>();
            foreach (var s in streets)
            {
                if (s.addresses != null && s.addresses.Count > 0)
                {
                    candidateStreets.Add(s);
                }
            }

            if (candidateStreets.Count == 0) return;

            var randomStreet = candidateStreets[Random.Range(0, candidateStreets.Count)];
            var randomAddr = randomStreet.addresses[Random.Range(0, randomStreet.addresses.Count)];

            _gpsSystem ??= GameManager.GetMonoSystem<IGPSMonoSystem>();
            if (_gpsSystem == null) return;

            var path = _gpsSystem.CalculatePath(transform.position, randomAddr.addressLabel);

            if (path != null && path.Count > 1)
            {
                _currentPath = new List<Vector3>(path);
                _pathIndex = 0;
                _destinationAddress = randomAddr.addressLabel;
                _isIdleCruising = true;
            }
        }

        public void Initialize(FieldUnitSO unitData, string callsign)
        {
            _unitData = unitData;
            _callsign = callsign;
        }

        public void AssignRoute(List<Vector3> path, string targetAddr, string callId)
        {
            if (path == null || path.Count == 0)
            {
                PlazmaDebug.LogWarning($"Received empty path to '{targetAddr}'.", $"Unit {_callsign}");
                return;
            }

            _isIdleCruising = false;
            _currentPath = new List<Vector3>(path);
            _pathIndex = 0;
            _destinationAddress = targetAddr;
            _activeCallId = callId;
            _status = UnitStatus.EnRoute;

            UpdateRouteVisuals();
        }

        public void SetAvailable()
        {
            _status = UnitStatus.Available;
            _isIdleCruising = false;
            _activeCallId = "";
            _destinationAddress = "None";
            _currentPath.Clear();
            _pathIndex = 0;
            _idleTimer = 0f;
            UpdateCurrentLocationLabel();
            UpdateRouteVisuals();
        }

        public void SetUnavailable()
        {
            _status = UnitStatus.Unavailable;
            _isIdleCruising = false;
            _activeCallId = "";
            _destinationAddress = "None";
            _currentPath.Clear();
            _pathIndex = 0;
            _idleTimer = 0f;
            UpdateRouteVisuals();
        }

        private void FollowPath()
        {
            if (_currentPath == null || _pathIndex >= _currentPath.Count)
            {
                OnReachedDestination();
                return;
            }

            Vector3 targetPoint = _currentPath[_pathIndex];

            Vector3 diff = targetPoint - transform.position;
            float horizontalDist = new Vector2(diff.x, diff.z).magnitude;

            if (horizontalDist <= _waypointThreshold || diff.magnitude <= _waypointThreshold)
            {
                _pathIndex++;
                if (_pathIndex >= _currentPath.Count)
                {
                    OnReachedDestination();
                    return;
                }
                targetPoint = _currentPath[_pathIndex];
                diff = targetPoint - transform.position;
            }

            float activeSpeed = _isIdleCruising ? _unitData.patrolSpeed : _unitData.moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, activeSpeed * Time.deltaTime);

            Vector3 flatDir = new Vector3(diff.x, 0f, diff.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, _unitData.rotationSpeed * Time.deltaTime);
            }
        }

        private void OnReachedDestination()
        {
            if (_isIdleCruising)
            {
                _isIdleCruising = false;
                _currentPath.Clear();
                _pathIndex = 0;
                _idleTimer = 0f;
                _destinationAddress = "None";
            }
            else
            {
                _status = UnitStatus.OnScene;
                _currentLocationLabel = _destinationAddress;
                _currentPath.Clear();
                _pathIndex = 0;
                PlazmaDebug.Log($" ON SCENE at {_destinationAddress} (Call {_activeCallId}).", $"Unit {_callsign}");
            }

            UpdateRouteVisuals();
        }

        private void SnapToStartingRoadPosition()
        {
            var streets = _streetSystem.GetNamedStreets();
            if (streets == null || streets.Count == 0) return;

            if (!string.IsNullOrEmpty(_startingAddress))
            {
                foreach (var street in streets)
                {
                    foreach (var addr in street.addresses)
                    {
                        if (addr.addressLabel.Equals(_startingAddress, System.StringComparison.OrdinalIgnoreCase) ||
                            addr.addressLabel.Contains(_startingAddress))
                        {
                            transform.position = addr.position;
                            return;
                        }
                    }
                }
            }

            float closestDist = float.MaxValue;
            Vector3 bestPos = transform.position;

            foreach (var street in streets)
            {
                foreach (var addr in street.addresses)
                {
                    float d = Vector3.Distance(transform.position, addr.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        bestPos = addr.position;
                    }
                }
            }

            transform.position = bestPos;
        }

        private void UpdateCurrentLocationLabel()
        {
            if (_streetSystem == null) return;

            float bestDist = float.MaxValue;
            string bestLabel = _currentLocationLabel;

            var nodes = _streetSystem.GetNodes();
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    if (!string.IsNullOrEmpty(node.intersectionName))
                    {
                        float d = Vector3.Distance(transform.position, node.position);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestLabel = node.intersectionName;
                        }
                    }
                }
            }

            var streets = _streetSystem.GetNamedStreets();
            if (streets != null)
            {
                foreach (var street in streets)
                {
                    foreach (var addr in street.addresses)
                    {
                        float d = Vector3.Distance(transform.position, addr.position);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            bestLabel = addr.addressLabel;
                        }
                    }
                }
            }

            _currentLocationLabel = bestLabel;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_status == UnitStatus.EnRoute && !_isIdleCruising && _currentPath != null && _pathIndex < _currentPath.Count)
            {
                Gizmos.color = _routeLineColor;
                Vector3 prev = transform.position;
                for (int i = _pathIndex; i < _currentPath.Count; i++)
                {
                    Vector3 next = _currentPath[i];
                    Gizmos.DrawLine(prev + Vector3.up * 0.25f, next + Vector3.up * 0.25f);
                    prev = next;
                }
            }

            Gizmos.color = _status switch
            {
                UnitStatus.Available => Color.green,
                UnitStatus.EnRoute => Color.cyan,
                UnitStatus.OnScene => Color.yellow,
                _ => Color.gray
            };

            Gizmos.DrawSphere(transform.position + Vector3.up * 0.5f, 0.8f);
            string callInfo = !string.IsNullOrEmpty(_activeCallId) ? $" ({_activeCallId})" : (_isIdleCruising ? " (Cruising)" : "");
            Handles.Label(transform.position + Vector3.up * 2.2f, $"[{_callsign}] {_status}{callInfo}\nLoc: {_currentLocationLabel}");
        }
#endif
    }
}