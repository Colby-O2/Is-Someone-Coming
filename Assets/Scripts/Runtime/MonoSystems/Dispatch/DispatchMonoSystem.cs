using UnityEngine;

using System.Collections.Generic;
using System.Linq;
using ColbyO.SJ26.Dialogue;
using ColbyO.SJ26.GameLogic;
using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Street;
using ColbyO.SJ26.GPS;
using ColbyO.SJ26.Map;
using Unity.VisualScripting;


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
    
        public class ActiveDispatchEvent
        {
            public DispatchEventSO Event;
            public string Address;
            public float StartTime;
            public EmergencyCallMarker Marker;
            public string CallId;
            
            public float ResolveStartTime = 0;
            public bool Resolving = false;

            public ActiveDispatchEvent(DispatchEventSO e, string address, EmergencyCallMarker marker, string callId)
            {
                Event = e;
                Address = address;
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

    public class DispatchMonoSystem : MonoBehaviour, IDispatchMonoSystem
    {

        
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

        private bool _takingCalls = true;
        private float _newCallCountDown = -1;

        private FieldUnit _selectedFieldUnit = null;

        private List<ActiveDispatchEvent> _events = new();
        private List<ActiveDispatchEvent> _eventsToRemove = new();

        private IGPSMonoSystem _gpsSystem;
        private IStreetMonoSystem _streetSystem;
        private bool _inCall = false;

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
        }

        private ActiveDispatchEvent CreateActiveDispatchEvent(DispatchEventSO e)
        {
            var streets = _streetSystem.GetNamedStreets();
            List<AddressMarker> spawnPoints = new ();

            if (streets != null)
            {
                foreach (var street in streets)
                {
                    if (street.addresses.Count > 0)
                    {
                        spawnPoints.AddRange(street.addresses);
                    }
                }
            }
            AddressMarker addr = spawnPoints[Random.Range(0, spawnPoints.Count)];

            Vector3 spawnPos = addr.position;
            
            var unitGO = new GameObject("Maker_Test");
            unitGO.transform.position = spawnPos;
            var marker = unitGO.AddComponent<EmergencyCallMarker>();
            var callId = "#" + Random.Range(0, 9999).ToString();
            var address = addr.addressLabel;
            marker.InitializeCall(_testCall, callId, address);
            marker.gameObject.SetActive(false);
            
            return new ActiveDispatchEvent(e, address, marker, callId);
        }

        public void StartEvent(ActiveDispatchEvent e)
        {
            e.Marker.gameObject.SetActive(true);
            _events.Add(e);
        }

        private void FailEvent(ActiveDispatchEvent e)
        {
            e.Marker.SetFailed(e);
            EndEvent(e);
        }

        private void SuccessEvent(ActiveDispatchEvent e)
        {
            GameManager.GetMonoSystem<IGameLogicMonoSystem>().IncrementCounter("CallsCompleted");
            e.Marker.SetSuccess(e);
            EndEvent(e);
        }

        private void EndEvent(ActiveDispatchEvent e)
        {
            foreach (FieldUnit u in _registeredUnits)
            {
                if (u.ActiveCallId == e.CallId)
                {
                    u.SetAvailable();
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
                        SuccessEvent(e);
                    }
                }
                else if (e.HasRequiredUnits(GetUnitsAtAddress(e.Address)))
                {
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

            if (_takingCalls)
            {
                _newCallCountDown -= Time.deltaTime;
                if (_newCallCountDown <= 0)
                {
                    _newCallCountDown = SJ26GameManager.Preferences.RandomCallTime();
                    StartCall();
                }
            }
        }

        private void StartCall()
        {
            if (GameManager.GetMonoSystem<IDialogueMonoSystem>().IsPlayingDialogue())
                return;
            if (_inCall) return;
            DispatchEventSO e = RandomDispatchEvent();
            ActiveDispatchEvent ae = CreateActiveDispatchEvent(e);
            SetDialogueContext(ae);
            _inCall = true;
            GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(e.CallDialogue, true)
                .Then(_ =>
                {
                    _inCall = false;
                });
            SJ26GameManager.GlobalScheduler.When(() =>
                GameManager.GetMonoSystem<IDialogueMonoSystem>().GetFlag("StartDispatching")
            ).Then(_ =>
            {
                StartEvent(ae);
            });
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

        public DispatchEventSO RandomDispatchEvent()
        {
            var es = SJ26GameManager.Preferences.DispatchEvents.GetAllEntries();
            return es[Random.Range(0, es.Count)];
        }

        private void SetDialogueContext(ActiveDispatchEvent e)
        {
            GameManager.GetMonoSystem<IDialogueMonoSystem>().SetFlag("StartDispatching", false);
            GameManager.GetMonoSystem<IDialogueMonoSystem>().SetVariable("Address", e.Address);
        }

        public void HandleMapObjectClick(MapObject mo)
        {
            Debug.Log(mo);
            if (mo is FieldUnit fu)
            {
                Debug.Log("SELECT");
                if (fu == _selectedFieldUnit)
                {
                    _selectedFieldUnit = null;
                    fu.SetSelected(false);
                }
                else
                {
                    if (_selectedFieldUnit != null)
                    {
                        _selectedFieldUnit.SetSelected(false);
                    }
                    _selectedFieldUnit = fu;
                    fu.SetSelected(true);
                }
            }
            else if (mo is EmergencyCallMarker ecm)
            {
                Debug.Log("TRY SEND");
                if (ecm.IsEnded())
                {
                    if (GameManager.GetMonoSystem<IDialogueMonoSystem>().IsPlayingDialogue())
                        return;
                    ActiveDispatchEvent e = ecm.FinishedEvent();
                    SetDialogueContext(e);
                    if (ecm.IsSuccess())
                    {
                        GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(e.Event.SuccessDialogue, true);
                    }
                    else
                    {
                        GameManager.GetMonoSystem<IDialogueMonoSystem>().StartDialoguePromise(e.Event.FailedDialogue, true);
                    }
                    GameObject.Destroy(ecm.gameObject);
                }
                else if (_selectedFieldUnit)
                {
                    DispatchUnitToAddress(_selectedFieldUnit, ecm.CurrentLocation, ecm.CallId());
                    _selectedFieldUnit.SetSelected(false);
                    _selectedFieldUnit = null;
                }
            }
        }

        public void StartTheCalls()
        {
            _newCallCountDown = SJ26GameManager.Preferences.RandomCallTime();
            _takingCalls = true;
        }

        public void StopTheCalls()
        {
            _takingCalls = false;
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