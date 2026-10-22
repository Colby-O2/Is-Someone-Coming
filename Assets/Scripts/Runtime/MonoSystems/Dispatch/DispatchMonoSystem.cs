using UnityEngine;

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Street;
using ColbyO.SJ26.GPS;
using ColbyO.SJ26.Map;
using DialogueGraph.Data;
using Unity.U2D.Physics;
using Unity.VisualScripting;
using static UnityEngine.Rendering.STP;



#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColbyO.SJ26.Dispatch
{
    [System.Serializable]
    public struct UnitSpawnConfig
    {
        public FieldUnitSO unitData;

        public UnitSpawnConfig(FieldUnitSO unitData)
        {
            this.unitData = unitData;
        }
    }
    
    public class DispatchEvent
    {
        public DialogueGraphSO Dialogue;
        public string Address;
        public int RequiredPolice;
        public int RequiredEMS;
        public int RequiredFire;
        public int RequiredRescue;
        public float TimeToRespond;
        public float TimeToResolve;
    }
    

    public class DispatchMonoSystem : MonoBehaviour, IDispatchMonoSystem
    {

        private class ActiveDispatchEvent
        {
            public DispatchEvent Event;
            public float StartTime;
            public EmergencyCallMarker Marker;
            public string CallId;
            
            public float ResolveStartTime = 0;
            public bool Resolving = false;

            public ActiveDispatchEvent(DispatchEvent e, EmergencyCallMarker marker, string callId)
            {
                Event = e;
                StartTime = Time.time;
                Marker = marker;
                CallId = callId;
            }

            public float ActiveTime() => Time.time - StartTime;
            
            public float TimeLeft() => Event.TimeToRespond - ActiveTime();
            
            public bool IsResolving() => Resolving;

            public bool IsResolved() =>
                IsResolving() &&
                Time.time - ResolveStartTime >= Event.TimeToResolve;
            
            public void StartResolving()
            {
                Resolving = true;
                ResolveStartTime = Time.time;
            }

            public bool HasRequiredUnits(List<UnitType> units)
            {
                return
                    units.Count(u => u == UnitType.Fire) == Event.RequiredFire &&
                    units.Count(u => u == UnitType.EMS) == Event.RequiredEMS &&
                    units.Count(u => u == UnitType.Police) == Event.RequiredPolice &&
                    units.Count(u => u == UnitType.Rescue) == Event.RequiredRescue;
            }

        }
        
        [Header("Testing & Auto-Spawn")]
        [SerializeField] private bool _autoSpawnUnitsOnStart = true;
        [SerializeField] private GameObject _optionalUnitPrefab;
        [SerializeField] MapObjectSO _testCall;
        [SerializeField] private List<UnitSpawnConfig> _defaultSpawnConfigs = new List<UnitSpawnConfig> { };

        [Header("Registered Field Units")]
        [SerializeField] private List<FieldUnit> _registeredUnits = new List<FieldUnit>();

        [Header("Test Dispatch Console")]
        [SerializeField] private string _testUnitCallsign = "P-01";
        [SerializeField] private string _testTargetAddress = "";
        [SerializeField] private string _testCallId = "#1047";

        private FieldUnit _selectedFieldUnit = null;

        private List<ActiveDispatchEvent> _events = new();
        private List<ActiveDispatchEvent> _eventsToRemove = new();

        private IGPSMonoSystem _gpsSystem;
        private IStreetMonoSystem _streetSystem;

        private void Start()
        {
            if (_autoSpawnUnitsOnStart)
            {
                SpawnInitialUnits();
            }
            // ---
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
            if (_streetSystem == null) return;

            var streets = _streetSystem.GetNamedStreets();
            List<Vector3> spawnPoints = new List<Vector3>();

            if (streets != null)
            {
                foreach (var street in streets)
                {
                    if (street.addresses.Count > 0)
                    {
                        spawnPoints.Add(street.addresses[0].position);
                    }
                }
            }

            GameObject unitGO;
            EmergencyCallMarker mapObj;

            Vector3 spawnPos = (spawnPoints.Count > 0)
            ? spawnPoints[0]
            : Vector3.zero;

            
            StartEvent(new DispatchEvent()
            {
                Address="fsad",
                RequiredFire = 1,
                RequiredPolice = 0,
                RequiredRescue = 0,
                RequiredEMS = 1,
                TimeToRespond = 40,
                TimeToResolve = 5,
            });
        }

        public void StartEvent(DispatchEvent e)
        {
            var streets = _streetSystem.GetNamedStreets();
            List<Vector3> spawnPoints = new List<Vector3>();

            if (streets != null)
            {
                foreach (var street in streets)
                {
                    if (street.addresses.Count > 0)
                    {
                        spawnPoints.Add(street.addresses[0].position);
                    }
                }
            }
            
            Vector3 spawnPos = (spawnPoints.Count > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Count)]
            : Vector3.zero;
            
            var unitGO = new GameObject("Maker_Test");
            unitGO.transform.position = spawnPos;
            var marker = unitGO.AddComponent<EmergencyCallMarker>();
            var callId = "#" + Random.Range(0, 9999).ToString();
            e.Address = streets[0].addresses[0].addressLabel;
            marker.InitializeCall(_testCall, callId, e.Address);
            
            _events.Add(new ActiveDispatchEvent(e, marker, callId));
        }

        private void FailEvent(ActiveDispatchEvent e)
        {
            e.Marker.SetFailed(e.Event);
            EndEvent(e);
        }

        private void SuccessEvent(ActiveDispatchEvent e)
        {
            e.Marker.SetSuccess(e.Event);
            EndEvent(e);
        }

        private void EndEvent(ActiveDispatchEvent e)
        {
            foreach (FieldUnit u in _registeredUnits)
            {
                if (u.ActiveCallId == e.CallId)
                {
                    u.TakeOffCall();
                }
            }
            _eventsToRemove.Add(e);
        }

        private List<UnitType> GetUnitsAtAddress(string address)
        {
            return _registeredUnits
                .Where(u => u.CurrentLocation == address)
                .Select(u => u.Type)
                .ToList();
        }

        private void Update()
        {
            _gpsSystem ??= GameManager.GetMonoSystem<IGPSMonoSystem>();
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();

            foreach (ActiveDispatchEvent e in _events)
            {
                if (e.IsResolving())
                {
                    if (e.IsResolved())
                    {
                        Debug.Log("SUCCESSSS!!!!!!!!!");
                        SuccessEvent(e);
                    }
                }
                else if (e.HasRequiredUnits(GetUnitsAtAddress(e.Event.Address)))
                {
                    Debug.Log("On Scene!!!!!!!!!!");
                    e.StartResolving();
                }
                
                if (e.TimeLeft() < 0)
                {
                    FailEvent(e);
                }
            }

            foreach (ActiveDispatchEvent e in _eventsToRemove)
            {
                _events.Remove(e);
            }
        }

        private void SpawnInitialUnits()
        {
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
            if (_streetSystem == null) return;

            var streets = _streetSystem.GetNamedStreets();
            List<Vector3> spawnPoints = new List<Vector3>();

            if (streets != null)
            {
                foreach (var street in streets)
                {
                    if (street.addresses.Count > 0)
                    {
                        spawnPoints.Add(street.addresses[0].position);
                    }
                }
            }

            int spawnIndex = 0;
            foreach (var config in _defaultSpawnConfigs)
            {
                Vector3 spawnPos = (spawnPoints.Count > 0)
                    ? spawnPoints[spawnIndex % spawnPoints.Count]
                    : Vector3.zero;

                spawnIndex++;

                GameObject unitGO;
                FieldUnit unit;

                if (_optionalUnitPrefab != null)
                {
                    unitGO = Instantiate(_optionalUnitPrefab, spawnPos, Quaternion.identity);
                    unit = unitGO.GetComponent<FieldUnit>();
                    if (unit == null) unit = unitGO.AddComponent<FieldUnit>();
                }
                else
                {
                    unitGO = new GameObject($"Unit_{config.unitData.callsignSymbol}-{(GetNumberOfRegisteredUnits(config.unitData.unitType, config.unitData.callsignSymbol) + 1):D2}");
                    unitGO.transform.position = spawnPos;
                    unit = unitGO.AddComponent<FieldUnit>();
                }

                string callsign = GetNextCallsign(
                    config.unitData.unitType,
                    config.unitData.callsignSymbol
                );

                unit.Initialize(config.unitData, callsign);
                RegisterUnit(unit);
            }
        }

        private string GetNextCallsign(UnitType unitType, string callsignSymbol)
        {
            int number = 1;

            while (GetUnitByCallsign($"{callsignSymbol}-{number:D2}") != null)
            {
                number++;
            }

            return $"{callsignSymbol}-{number:D2}";
        }

        public void RegisterUnit(FieldUnit unit)
        {
            if (unit != null && !_registeredUnits.Contains(unit))
            {
                _registeredUnits.Add(unit);
            }
        }

        public void UnregisterUnit(FieldUnit unit)
        {
            if (unit != null)
            {
                _registeredUnits.Remove(unit);
            }
        }

        public IReadOnlyList<FieldUnit> GetAllUnits() => _registeredUnits;

        public FieldUnit GetUnitByCallsign(string callsign)
        {
            if (string.IsNullOrEmpty(callsign)) return null;

            foreach (var unit in _registeredUnits)
            {
                if (unit != null && unit.Callsign.Equals(callsign, System.StringComparison.OrdinalIgnoreCase))
                {
                    return unit;
                }
            }
            return null;
        }

        public bool DispatchUnitToAddress(string unitCallsign, string targetAddress, string callId = "")
        {
            FieldUnit unit = GetUnitByCallsign(unitCallsign);
            if (unit == null)
            {
                PlazmaDebug.LogWarning($"Could not find unit with _callsign '{unitCallsign}'.", "DispatchMonoSystem");
                return false;
            }

            return DispatchUnitToAddress(unit, targetAddress, callId);
        }

        public bool DispatchUnitToAddress(FieldUnit unit, string targetAddress, string callId = "")
        {
            if (unit == null || string.IsNullOrEmpty(targetAddress)) return false;

            _gpsSystem ??= GameManager.GetMonoSystem<IGPSMonoSystem>();
            if (_gpsSystem == null)
            {
                PlazmaDebug.LogError("IGPSMonoSystem is not available via GameManager!", "DispatchMonoSystem");
                return false;
            }

            List<Vector3> route = _gpsSystem.CalculatePath(unit.transform.position, targetAddress);
            if (route == null || route.Count == 0)
            {
                PlazmaDebug.LogWarning($"Failed to calculate GPS route for {unit.Callsign} to {targetAddress}.", "DispatchMonoSystem");
                return false;
            }

            unit.AssignRoute(route, targetAddress, callId);
            PlazmaDebug.Log($"Dispatched {unit.Callsign} ({unit.Type}) to {targetAddress} for Call {callId}.", "DispatchMonoSystem");
            return true;
        }

        public int GetNumberOfRegisteredUnits(UnitType? unitType = null, string callsignSymbol = null)
        {
            int numOfUnits = 0;

            if (unitType == null && callsignSymbol == null) return numOfUnits;

            _registeredUnits.ForEach(unit =>
            {
                if (
                (!unitType.HasValue || unitType.Value == unit.Type) &&
                (callsignSymbol == null || callsignSymbol == string.Empty || callsignSymbol == unit.CallsignSymbol)
                ) numOfUnits++;
            });

            return numOfUnits;
        }

        public void HandleMapObjectClick(MapObject mo)
        {
            Debug.Log(mo);
            if (mo is FieldUnit fu)
            {
                Debug.Log("SELECT");
                _selectedFieldUnit = fu == _selectedFieldUnit ? null : fu;
            }
            else if (mo is EmergencyCallMarker ecm)
            {
                Debug.Log("TRY SEND");
                if (ecm.IsEnded())
                {
                    GameObject.Destroy(ecm.gameObject);
                }
                else if (_selectedFieldUnit)
                {
                    DispatchUnitToAddress(_selectedFieldUnit, ecm.CurrentLocation);
                }
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DispatchMonoSystem))]
    public class DispatchMonoSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DispatchMonoSystem dispatchSys = (DispatchMonoSystem)target;

            List<string> unitOptions = new List<string> { "" };
            foreach (var u in dispatchSys.GetAllUnits())
            {
                if (u != null) unitOptions.Add(u.Callsign);
            }

            List<string> addressOptions = new List<string> { "" };
            if (Application.isPlaying)
            {
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
            }

            SerializedProperty unitProp = serializedObject.FindProperty("_testUnitCallsign");
            SerializedProperty addrProp = serializedObject.FindProperty("_testTargetAddress");
            SerializedProperty callIdProp = serializedObject.FindProperty("_testCallId");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live Dispatch Testing Console", EditorStyles.boldLabel);

            if (Application.isPlaying && unitOptions.Count > 1)
            {
                int uIndex = Mathf.Max(0, unitOptions.IndexOf(unitProp.stringValue));
                uIndex = EditorGUILayout.Popup("Select Field Unit", uIndex, unitOptions.ToArray());
                unitProp.stringValue = unitOptions[uIndex];
            }
            else
            {
                EditorGUILayout.PropertyField(unitProp, new GUIContent("Unit Callsign"));
            }

            if (Application.isPlaying && addressOptions.Count > 1)
            {
                int aIndex = Mathf.Max(0, addressOptions.IndexOf(addrProp.stringValue));
                aIndex = EditorGUILayout.Popup("Target Address", aIndex, addressOptions.ToArray());
                addrProp.stringValue = addressOptions[aIndex];
            }
            else
            {
                EditorGUILayout.PropertyField(addrProp, new GUIContent("Target Address"));
            }

            EditorGUILayout.PropertyField(callIdProp, new GUIContent("Call ID"));

            serializedObject.ApplyModifiedProperties();

            GUI.enabled = Application.isPlaying;
            if (GUILayout.Button("Dispatch Selected Unit"))
            {
                dispatchSys.DispatchUnitToAddress(unitProp.stringValue, addrProp.stringValue, callIdProp.stringValue);
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
#endif
}