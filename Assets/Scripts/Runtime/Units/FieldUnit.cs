using UnityEngine;

using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Attribute;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Map;
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
        Fire,
        Rescue,
    }

    public enum UnitStatus
    {
        Available,
        EnRoute,
        OnScene,
        Unavailable
    }

    public class FieldUnit : MapObject
    {
        [Header("Unit Identity")]
        [SerializeField, ReadOnly] private string _callsign = "P-01";
        [SerializeField, ReadOnly] private UnitStatus _status = UnitStatus.Available;

        [Header("Route Renderer Settings")]
        [SerializeField] private Material _routeLineMaterial;
        [SerializeField] private Color _routeLineColor = new Color(0f, 0.75f, 1f, 1f);
        [SerializeField] private float _routeLineWidth = 1f;
        [SerializeField] private float _routeLineHeightOffset = 0.5f;

        [Header("Movement Settings")]
        [SerializeField] private float _waypointThreshold = 0.5f;

        [Header("Live Unit Telemetry")]
        [SerializeField, ReadOnly] private string _activeCallId = "";
        [SerializeField, ReadOnly] private string _destinationAddress = "None";

        private LineRenderer _routeLineRenderer;
        private float _idleTimer = 0f;
        private bool _isIdleCruising = false;

        private List<Vector3> _currentPath = new List<Vector3>();
        private int _pathIndex = 0;

        private IDispatchMonoSystem _dispatchSystem;
        private IGPSMonoSystem _gpsSystem;

        public FieldUnitSO UnitData => _mapObjectData as FieldUnitSO;
        public string CallsignSymbol => UnitData != null ? UnitData.callsignSymbol : "";
        public string Callsign => _callsign;
        public UnitType Type => UnitData != null ? UnitData.unitType : UnitType.Police;
        public UnitStatus Status => _status;
        public string ActiveCallId => _activeCallId;
        public string Destination => _destinationAddress;
        public IReadOnlyList<Vector3> CurrentPath => _currentPath;
        public int CurrentPathIndex => _pathIndex;

        protected override void Start()
        {
            _dispatchSystem = GameManager.GetMonoSystem<IDispatchMonoSystem>();
            _gpsSystem = GameManager.GetMonoSystem<IGPSMonoSystem>();

            _dispatchSystem?.RegisterUnit(this);

            base.Start();

            SetupRouteRenderer();
        }

        private void OnDestroy()
        {
            _dispatchSystem ??= GameManager.GetMonoSystem<IDispatchMonoSystem>();
            _dispatchSystem?.UnregisterUnit(this);
        }

        protected override void Update()
        {
            _dispatchSystem ??= GameManager.GetMonoSystem<IDispatchMonoSystem>();
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

            base.Update();
            UpdateRouteVisuals();
        }

        protected override bool AreObjectsStacked(MapObject other)
        {
            if (base.AreObjectsStacked(other)) return true;

            if (other is FieldUnit otherUnit)
            {
                if (_status == UnitStatus.OnScene && otherUnit.Status == UnitStatus.OnScene &&
                    !string.IsNullOrEmpty(_currentLocationLabel) &&
                    _currentLocationLabel == otherUnit.CurrentLocation)
                {
                    return true;
                }
            }

            return false;
        }

        public void Initialize(FieldUnitSO unitData, string callsign)
        {
            _mapObjectData = unitData;
            _callsign = callsign;
            SetupIcon();
        }

        private void SetupRouteRenderer()
        {
            GameObject lineGO = new GameObject($"RouteLine_{_callsign}");
            lineGO.transform.SetParent(transform, false);
            if (_iconPrefab != null)
            {
                lineGO.layer = _iconPrefab.layer;
            }

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
            if (UnitData == null || !UnitData.autoPatrolWhenAvailable)
                return;

            _idleTimer += Time.deltaTime;
            if (_idleTimer >= UnitData.idleWaitDuration)
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

            float activeSpeed = _isIdleCruising ? UnitData.patrolSpeed : UnitData.moveSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targetPoint, activeSpeed * Time.deltaTime);

            Vector3 flatDir = new Vector3(diff.x, 0f, diff.z);
            if (flatDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, UnitData.rotationSpeed * Time.deltaTime);
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

            Vector3 gizmoPos = transform.position + _currentVisualSpreadOffset;
            Gizmos.DrawSphere(gizmoPos + Vector3.up * 0.5f, 0.8f);
            string callInfo = !string.IsNullOrEmpty(_activeCallId) ? $" ({_activeCallId})" : (_isIdleCruising ? " (Cruising)" : "");
            Handles.Label(gizmoPos + Vector3.up * 2.2f, $"[{_callsign}] {_status}{callInfo}\nLoc: {_currentLocationLabel}");
        }
#endif
    }
}