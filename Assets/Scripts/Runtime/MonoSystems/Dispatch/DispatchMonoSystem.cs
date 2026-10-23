using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlazmaGames.Core;
using PlazmaGames.Core.Debugging;
using ColbyO.SJ26.Dialogue;
using ColbyO.SJ26.GameLogic;
using ColbyO.SJ26.GPS;
using ColbyO.SJ26.Map;
using ColbyO.SJ26.Street;
using InteractionSystem;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColbyO.SJ26.Dispatch
{
    [Serializable]
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
        public DispatchEventSO Event { get; }
        public string Address { get; }
        public float StartTime { get; private set; }
        public EmergencyCallMarker Marker { get; }
        public string CallId { get; }
        public string LocDescription { get; }

        public float ResolveStartTime { get; private set; }
        public bool Resolving { get; private set; }
        public bool Answered = false;
        public bool Cancel = false;

        public ActiveDispatchEvent(DispatchEventSO e, string address, EmergencyCallMarker marker, string callId)
        {
            Event = e;
            Address = address;
            StartTime = Time.time;
            Marker = marker;
            CallId = callId;
            LocDescription = (Event.LocDescriptions != null && Event.LocDescriptions.Count > 0)
                ? Event.LocDescriptions[Random.Range(0, Event.LocDescriptions.Count)]
                : string.Empty;
            Resolving = false;
        }

        public void ResetTimer()
        {
            StartTime = Time.time;
        }

        public float ActiveTime() => Resolving
            ? (ResolveStartTime - StartTime)
            : (Time.time - StartTime);

        public float TimeLeft() => Event.TimeToRespond - ActiveTime();

        public bool IsFailed() => !IsResolving() && TimeLeft() < 0f;

        public bool IsResolving() => Resolving;

        public bool IsResolved() =>
            Resolving && (Time.time - ResolveStartTime >= Event.TimeToResolve);

        public void StartResolving()
        {
            if (Resolving) return;

            Resolving = true;
            ResolveStartTime = Time.time;

            if (Marker != null)
            {
                Marker.SetResolving(true);
            }
        }

        public bool HasRequiredUnits(IReadOnlyList<FieldUnit> allUnits)
        {
            int fire = 0, ems = 0, police = 0, rescue = 0;

            for (int i = 0; i < allUnits.Count; i++)
            {
                FieldUnit unit = allUnits[i];
                if (unit == null || unit.CurrentLocation != Address) continue;

                switch (unit.Type)
                {
                    case UnitType.Fire: fire++; break;
                    case UnitType.EMS: ems++; break;
                    case UnitType.Police: police++; break;
                    case UnitType.Rescue: rescue++; break;
                }
            }

            return fire >= Event.RequiredFire &&
                   ems >= Event.RequiredEMS &&
                   police >= Event.RequiredPolice &&
                   rescue >= Event.RequiredRescue;
        }
    }

    public class DispatchMonoSystem : MonoBehaviour, IDispatchMonoSystem
    {
        [Header("Auto-Spawn")]
        [SerializeField] private bool _autoSpawnUnitsOnStart = true;
        [SerializeField] private GameObject _optionalUnitPrefab;
        [SerializeField] private MapObjectSO _testCall;
        [SerializeField] private List<UnitSpawnConfig> _defaultSpawnConfigs = new();

        [Header("Registered Field Units")]
        [SerializeField] private List<FieldUnit> _registeredUnits = new();

        [Header("Test Dispatch Console")]
        [SerializeField] private string _testUnitCallsign = "P-01";
        [SerializeField] private string _testTargetAddress = "";
        [SerializeField] private string _testCallId = "#1047";

        private readonly List<ActiveDispatchEvent> _events = new();

        private IGPSMonoSystem _gpsSystem;
        private IStreetMonoSystem _streetSystem;
        private IDialogueMonoSystem _dialogueSystem;
        private IGameLogicMonoSystem _gameLogicSystem;

        private FieldUnit _selectedFieldUnit;
        private bool _takingCalls = false;
        private bool _inCall;
        private float _newCallCountDown = 4f;

        private IGPSMonoSystem GpsSystem => _gpsSystem ??= GameManager.GetMonoSystem<IGPSMonoSystem>();
        private IStreetMonoSystem StreetSystem => _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
        private IDialogueMonoSystem DialogueSystem => _dialogueSystem ??= GameManager.GetMonoSystem<IDialogueMonoSystem>();
        private IGameLogicMonoSystem GameLogicSystem => _gameLogicSystem ??= GameManager.GetMonoSystem<IGameLogicMonoSystem>();

        private PhoneAction _phone;
        private AudioSource _phoneRinger;
        private ActiveDispatchEvent _stagedEvent = null;

        private void Start()
        {
            if (_autoSpawnUnitsOnStart)
            {
                SpawnInitialUnits();
            }

            GameObject phoneObj = GameObject.FindWithTag("Phone");
            if (phoneObj != null)
            {
                if (phoneObj.TryGetComponent<Interactable>(out var interactable))
                {
                    _phone = interactable.GetAction<PhoneAction>();
                    if (_phone != null) _phone.IsEnabled = false;
                }

                _phoneRinger = phoneObj.GetComponent<AudioSource>();
                if (_phoneRinger != null)
                {
                    _phoneRinger.Play();
                    _phoneRinger.Pause();
                }
            }
        }

        private void Update()
        {
            UpdateActiveEvents();
            UpdateCallTimer();
        }

        private void UpdateActiveEvents()
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                ActiveDispatchEvent e = _events[i];

                if (!e.IsResolving() && e.HasRequiredUnits(_registeredUnits))
                {
                    e.StartResolving();
                }

                if (e.IsResolving())
                {
                    if (e.IsResolved())
                    {
                        SuccessEvent(e);
                        _events.RemoveAt(i);
                    }
                }
                else
                {
                    float timeLeft = e.TimeLeft();

                    if (timeLeft < 0f)
                    {
                        FailEvent(e);
                        _events.RemoveAt(i);
                    }
                    else if (e.Marker != null)
                    {
                        e.Marker.UpdateTimerVisuals(timeLeft, e.Event.TimeToRespond);
                    }
                }
            }
        }

        private void UpdateCallTimer()
        {
            if (!_takingCalls || _stagedEvent != null || _inCall) return;
            if (DialogueSystem != null && DialogueSystem.IsPlayingDialogue()) return;

            _newCallCountDown -= Time.deltaTime;
            if (_newCallCountDown <= 0f)
            {
                _newCallCountDown = SJ26GameManager.Preferences.RandomCallTime();
                StartCall();
            }
        }

        private ActiveDispatchEvent CreateActiveDispatchEvent(DispatchEventSO e)
        {
            var streets = StreetSystem?.GetNamedStreets();
            List<AddressMarker> spawnPoints = new();

            if (streets != null)
            {
                foreach (var street in streets)
                {
                    if (street.addresses != null && street.addresses.Count > 0)
                    {
                        spawnPoints.AddRange(street.addresses);
                    }
                }
            }

            if (spawnPoints.Count == 0)
            {
                PlazmaDebug.LogError("No valid address spawn points found to create dispatch event!", "DispatchMonoSystem");
                return null;
            }

            AddressMarker addr = spawnPoints[Random.Range(0, spawnPoints.Count)];
            string callId = $"#{Random.Range(0, 9999):D4}";
            string address = addr.addressLabel;

            GameObject markerGO = new GameObject($"Marker_{callId}");
            markerGO.transform.position = addr.position;

            EmergencyCallMarker marker = markerGO.AddComponent<EmergencyCallMarker>();
            marker.InitializeCall(_testCall, callId, address);

            marker.gameObject.SetActive(false);

            return new ActiveDispatchEvent(e, address, marker, callId);
        }

        public void StartEvent(ActiveDispatchEvent e)
        {
            Debug.Log($"START EVENT: {e?.CallId} at {e?.Address}");
            if (e?.Marker == null) return;

            if (e.Cancel || e.Event.TotalUnits() == 0)
            {
                Debug.Log("CANCEL OR 0 UNITS REQUIRED");
                if (e.Marker.gameObject != null) Destroy(e.Marker.gameObject);
                return;
            }

            e.Marker.gameObject.SetActive(true);
            e.Marker.SetupIcon();

            if (e.IsFailed())
            {
                Debug.Log("FAILED BEFORE DISPATCH");
                FailEvent(e);
            }
            else if (e.IsResolved())
            {
                Debug.Log("RESOLVED");
                SuccessEvent(e);
            }
            else if (!_events.Contains(e))
            {
                Debug.Log("ADDED TO ACTIVE EVENTS");
                _events.Add(e);
            }
        }

        private void FailEvent(ActiveDispatchEvent e)
        {
            if (e.Marker != null)
            {
                e.Marker.SetResolving(false);
                e.Marker.SetFailed(e);
            }
        }

        private void SuccessEvent(ActiveDispatchEvent e)
        {
            GameLogicSystem?.IncrementCounter("CallsCompleted");
            if (e.Marker != null)
            {
                e.Marker.SetResolving(false);
                e.Marker.SetSuccess(e);
            }
        }

        private void ReleaseAssignedUnits(ActiveDispatchEvent e)
        {
            if (e == null) return;

            foreach (FieldUnit unit in _registeredUnits)
            {
                if (unit != null && (unit.ActiveCallId == e.CallId || unit.CurrentLocation == e.Address))
                {
                    unit.SetAvailable();
                }
            }
        }

        public void StartCallWith(DispatchEventSO eventSO)
        {
            if (eventSO == null) return;

            if (_stagedEvent != null)
            {
                _stagedEvent.Cancel = true;
                if (_stagedEvent.Marker != null)
                {
                    Destroy(_stagedEvent.Marker.gameObject);
                }
            }

            ActiveDispatchEvent activeEvent = CreateActiveDispatchEvent(eventSO);
            _stagedEvent = activeEvent;
            if (_stagedEvent == null) return;

            Promise.Any(
                SJ26GameManager.GlobalScheduler.When(() => _stagedEvent != activeEvent || activeEvent.Cancel),
                SJ26GameManager.GlobalScheduler.When(() => activeEvent.IsFailed())
            ).Then(_ =>
            {
                if (activeEvent == null || activeEvent.Answered) return;

                if (activeEvent.Cancel)
                {
                    if (activeEvent.Marker != null) Destroy(activeEvent.Marker.gameObject);
                    return;
                }

                if (_stagedEvent == activeEvent)
                {
                    _stagedEvent = null;
                }

                if (_phone != null) _phone.IsEnabled = false;
                if (_phoneRinger != null) _phoneRinger.Pause();

                StartEvent(activeEvent);

                if (DialogueSystem != null && !DialogueSystem.IsPlayingDialogue())
                {
                    DialogueSystem.StartDialoguePromise("CallEnded", true);
                }
            });

            if (_phone != null) _phone.IsEnabled = true;
            if (_phoneRinger != null) _phoneRinger.UnPause();
        }

        public void MakeFailActiveEvents()
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                FailEvent(_events[i]);
                _events.RemoveAt(i);
            }
        }

        public void RemoveAllEvents()
        {
            for (int i = _events.Count - 1; i >= 0; i--)
            {
                ActiveDispatchEvent e = _events[i];
                e.Cancel = true;
                if (e.Marker != null && e.Marker.gameObject != null)
                {
                    Destroy(e.Marker.gameObject);
                }
                _events.RemoveAt(i);
            }
        }

        public bool PhoneIsRinging()
        {
            return _stagedEvent != null;
        }

        private void StartCall()
        {
            if (_stagedEvent != null || _inCall || DialogueSystem == null || DialogueSystem.IsPlayingDialogue())
            {
                _newCallCountDown = 3f;
                return;
            }

            DispatchEventSO eventSO = RandomDispatchEvent();
            if (eventSO == null) return;
            StartCallWith(eventSO);
        }

        private void SpawnInitialUnits()
        {
            if (StreetSystem == null) return;

            var streets = StreetSystem.GetNamedStreets();
            List<Vector3> spawnPoints = new();

            if (streets != null)
            {
                foreach (var street in streets)
                {
                    if (street.addresses != null && street.addresses.Count > 0)
                    {
                        spawnPoints.Add(street.addresses[0].position);
                    }
                }
            }

            int spawnIndex = 0;
            foreach (var config in _defaultSpawnConfigs)
            {
                if (config.unitData == null) continue;

                Vector3 spawnPos = spawnPoints.Count > 0
                    ? spawnPoints[spawnIndex % spawnPoints.Count]
                    : Vector3.zero;

                spawnIndex++;

                string callsign = GetNextCallsign(config.unitData.unitType, config.unitData.callsignSymbol);
                GameObject unitGO;
                FieldUnit unit;

                if (_optionalUnitPrefab != null)
                {
                    unitGO = Instantiate(_optionalUnitPrefab, spawnPos, Quaternion.identity);
                    if (!unitGO.TryGetComponent(out unit))
                    {
                        unit = unitGO.AddComponent<FieldUnit>();
                    }
                }
                else
                {
                    unitGO = new GameObject($"Unit_{callsign}");
                    unitGO.transform.position = spawnPos;
                    unit = unitGO.AddComponent<FieldUnit>();
                }

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
                if (unit != null && string.Equals(unit.Callsign, callsign, StringComparison.OrdinalIgnoreCase))
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
                PlazmaDebug.LogWarning($"Could not find unit with callsign '{unitCallsign}'.", "DispatchMonoSystem");
                return false;
            }

            return DispatchUnitToAddress(unit, targetAddress, callId);
        }

        public bool DispatchUnitToAddress(FieldUnit unit, string targetAddress, string callId = "")
        {
            if (unit == null || string.IsNullOrEmpty(targetAddress)) return false;

            if (GpsSystem == null)
            {
                PlazmaDebug.LogError("IGPSMonoSystem is not available via GameManager!", "DispatchMonoSystem");
                return false;
            }

            List<Vector3> route = GpsSystem.CalculatePath(unit.transform.position, targetAddress);
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
            if (unitType == null && string.IsNullOrEmpty(callsignSymbol)) return 0;

            int count = 0;
            foreach (var unit in _registeredUnits)
            {
                if (unit == null) continue;

                bool typeMatches = !unitType.HasValue || unitType.Value == unit.Type;
                bool symbolMatches = string.IsNullOrEmpty(callsignSymbol) || callsignSymbol == unit.CallsignSymbol;

                if (typeMatches && symbolMatches)
                {
                    count++;
                }
            }
            return count;
        }

        public DispatchEventSO RandomDispatchEvent()
        {
            var entries = SJ26GameManager.Preferences.DispatchEvents.GetAllEntries().Where(e => e.UseInRandom).ToList();
            if (entries == null || entries.Count == 0) return null;

            return entries[Random.Range(0, entries.Count)];
        }

        private void SetDialogueContext(ActiveDispatchEvent e)
        {
            if (DialogueSystem == null || e == null) return;

            DialogueSystem.SetFlag("StartDispatching", false);
            DialogueSystem.SetVariable("Address", e.Address);
            DialogueSystem.SetVariable("CallID", e.CallId);
            DialogueSystem.SetVariable("EventLocDes", e.LocDescription);
        }

        public void HandleMapObjectClick(MapObject mo)
        {
            if (mo is FieldUnit clickedUnit)
            {
                HandleUnitSelection(clickedUnit);
            }
            else if (mo is EmergencyCallMarker callMarker)
            {
                HandleCallMarkerClick(callMarker);
            }
        }

        private void HandleUnitSelection(FieldUnit clickedUnit)
        {
            if (clickedUnit == _selectedFieldUnit)
            {
                _selectedFieldUnit.SetSelected(false);
                _selectedFieldUnit = null;
                return;
            }

            if (_selectedFieldUnit != null)
            {
                _selectedFieldUnit.SetSelected(false);
            }

            _selectedFieldUnit = clickedUnit;
            _selectedFieldUnit.SetSelected(true);
        }

        private void HandleCallMarkerClick(EmergencyCallMarker marker)
        {
            if (marker.IsEnded())
            {
                Debug.Log("ECM ENDED");
                ActiveDispatchEvent finishedEvent = marker.FinishedEvent();

                ReleaseAssignedUnits(finishedEvent);

                if (finishedEvent != null && DialogueSystem != null && !DialogueSystem.IsPlayingDialogue())
                {
                    SetDialogueContext(finishedEvent);

                    var dialogue = marker.IsSuccess()
                        ? finishedEvent.Event.SuccessDialogue
                        : finishedEvent.Event.FailedDialogue;

                    DialogueSystem?.StartDialoguePromise(dialogue, true);
                }

                if (marker.gameObject != null) Destroy(marker.gameObject);
            }
            else if (_selectedFieldUnit != null)
            {
                DispatchUnitToAddress(_selectedFieldUnit, marker.CurrentLocation, marker.CallId());
                _selectedFieldUnit.SetSelected(false);
                _selectedFieldUnit = null;
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

        public void AnswerPhone()
        {
            if (_stagedEvent == null || DialogueSystem == null || DialogueSystem.IsPlayingDialogue()) return;

            if (_phone != null) _phone.IsEnabled = false;
            if (_phoneRinger != null) _phoneRinger.Pause();

            ActiveDispatchEvent activeEvent = _stagedEvent;
            _stagedEvent = null;

            activeEvent.Answered = true;
            activeEvent.ResetTimer();

            SetDialogueContext(activeEvent);
            _inCall = true;

            DialogueSystem.StartDialoguePromise(activeEvent.Event.CallDialogue, true)
                .Then(_ => _inCall = false);

            Promise.Any(
                SJ26GameManager.GlobalScheduler.When(() => _inCall == false),
                SJ26GameManager.GlobalScheduler.When(() => activeEvent.IsFailed()),
                SJ26GameManager.GlobalScheduler.When(() => activeEvent.IsResolved())
            ).Then(_ =>
            {
                if (_inCall && !activeEvent.Cancel)
                {
                    _inCall = false;
                    DialogueSystem.FinishDialogue();
                    if (activeEvent.IsResolved())
                    {
                        DialogueSystem.StartDialoguePromise("CallResolved", true);
                    }
                    else
                    {
                        DialogueSystem.StartDialoguePromise("CallEnded", true);
                    }
                }
            });

            Promise.Any(
                SJ26GameManager.GlobalScheduler.When(() => DialogueSystem.GetFlag("StartDispatching")),
                SJ26GameManager.GlobalScheduler.When(() => _inCall == false)
            ).Then(_ => StartEvent(activeEvent));
        }

        public void HideAllUnits()
        {
            foreach (var ecm in FindObjectsByType<EmergencyCallMarker>(FindObjectsSortMode.None))
            {
                if (ecm != null && ecm.gameObject != null) Destroy(ecm.gameObject);
            }
            foreach (FieldUnit unit in _registeredUnits)
            {
                if (unit != null) unit.gameObject.SetActive(false);
            }
        }

        public void UnHideAllUnits()
        {
            foreach (FieldUnit unit in _registeredUnits)
            {
                if (unit != null) unit.gameObject.SetActive(true);
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(DispatchMonoSystem))]
    public class DispatchMonoSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DispatchMonoSystem dispatchSys = (DispatchMonoSystem)target;

            List<string> unitOptions = new() { "" };
            foreach (var u in dispatchSys.GetAllUnits())
            {
                if (u != null) unitOptions.Add(u.Callsign);
            }

            List<string> addressOptions = new() { "" };
            if (Application.isPlaying)
            {
                IStreetMonoSystem streetSys = GameManager.GetMonoSystem<IStreetMonoSystem>();
                var namedStreets = streetSys?.GetNamedStreets();
                if (namedStreets != null)
                {
                    foreach (var street in namedStreets)
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