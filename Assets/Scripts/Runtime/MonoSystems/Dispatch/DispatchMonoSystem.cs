using System.Collections.Generic;
using UnityEngine;
using PlazmaGames.Core;
using ColbyO.SJ26.Street;
using ColbyO.SJ26.GPS;
using PlazmaGames.Core.Debugging;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColbyO.SJ26.Dispatch
{
    [System.Serializable]
    public struct UnitSpawnConfig
    {
        public string callsign;
        public UnitType unitType;
        public UnitStatus initialStatus;

        public UnitSpawnConfig(string callsign, UnitType unitType, UnitStatus initialStatus = UnitStatus.Available)
        {
            this.callsign = callsign;
            this.unitType = unitType;
            this.initialStatus = initialStatus;
        }
    }

    public class DispatchMonoSystem : MonoBehaviour, IDispatchMonoSystem
    {
        [Header("Testing & Auto-Spawn")]
        [SerializeField] private bool _autoSpawnUnitsOnStart = true;
        [SerializeField] private GameObject _optionalUnitPrefab;
        [SerializeField]
        private List<UnitSpawnConfig> _defaultSpawnConfigs = new List<UnitSpawnConfig>
        {
            new UnitSpawnConfig("P-01", UnitType.Police, UnitStatus.Available),
            new UnitSpawnConfig("P-02", UnitType.Police, UnitStatus.Available),
            new UnitSpawnConfig("P-03", UnitType.Police, UnitStatus.Available),
            new UnitSpawnConfig("E-01", UnitType.EMS, UnitStatus.Available),
            new UnitSpawnConfig("E-02", UnitType.EMS, UnitStatus.Available),
            new UnitSpawnConfig("PD-01", UnitType.Other, UnitStatus.Available)
        };

        [Header("Registered Field Units")]
        [SerializeField] private List<FieldUnit> _registeredUnits = new List<FieldUnit>();

        [Header("Test Dispatch Console")]
        [SerializeField] private string _testUnitCallsign = "P-01";
        [SerializeField] private string _testTargetAddress = "";
        [SerializeField] private string _testCallId = "#1047";

        private IGPSMonoSystem _gpsSystem;
        private IStreetMonoSystem _streetSystem;

        private void Start()
        {
            if (_autoSpawnUnitsOnStart)
            {
                SpawnInitialUnits();
            }
        }

        private void Update()
        {
            _gpsSystem ??= GameManager.GetMonoSystem<IGPSMonoSystem>();
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
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
                if (GetUnitByCallsign(config.callsign) != null) continue;

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
                    unitGO = new GameObject($"Unit_{config.callsign}");
                    unitGO.transform.position = spawnPos;
                    unit = unitGO.AddComponent<FieldUnit>();
                }

                InitializeUnitProperties(unit, config.callsign, config.unitType, config.initialStatus);
                RegisterUnit(unit);
            }
        }

        private void InitializeUnitProperties(FieldUnit unit, string callsign, UnitType type, UnitStatus status)
        {
            var typeField = typeof(FieldUnit).GetField("_unitType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var callsignField = typeof(FieldUnit).GetField("_callsign", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var statusField = typeof(FieldUnit).GetField("_status", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            callsignField?.SetValue(unit, callsign);
            typeField?.SetValue(unit, type);
            statusField?.SetValue(unit, status);
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