using DialogueGraph.Attribute;
using DialogueGraph.Data;
using DialogueGraph.Enumeration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace DialogueGraph
{
    public abstract class DialogueController : MonoBehaviour
    {
        [Header("Database")]
        [SerializeField] protected DialogueGraphDatabase _db;

        [Header("Camera Settings")]
        [SerializeField] protected string _cinematicCameraTag = "MainCamera";

        protected Dictionary<string, DialogueExposedPropertiesData> _exposedVariables = new Dictionary<string, DialogueExposedPropertiesData>();
        protected Dictionary<string, UnityEvent<int>> _events = new Dictionary<string, UnityEvent<int>>();
        protected Dictionary<string, Transform> _cameraLocations = new Dictionary<string, Transform>();
        protected Dictionary<string, DialogueGraphSO> _dialogueGrapths;

        [Header("Debugging")]
        [SerializeField, ReadOnly] protected DialogueGraphSO _currentDialogue;
        [SerializeField, ReadOnly] protected DialogueNodeData _currentDialogueNode;
        [SerializeField, ReadOnly] protected DialogueType _currentNodeType;
        [SerializeField, ReadOnly] protected bool _isDialogueInProgress;

        private Camera _cinematicCamera;
        protected System.Action _completelyFinishedCallback;

        protected virtual void Awake()
        {
            DisabeCinematicCamera();
            InitializeDialogues();
        }

        public bool IsPlayingDialogue()
        {
            return _isDialogueInProgress;
        }

        public void AddListener(string eventTag, UnityAction<int> callback)
        {
            if (!_events.ContainsKey(eventTag)) _events.Add(eventTag, new UnityEvent<int>());
            _events[eventTag].AddListener(callback);
        }

        public bool GetFlag(string name)
        {
            if (_exposedVariables.ContainsKey(name))
            {
                if (_exposedVariables[name].Type == "Bool") return _exposedVariables[name].BooleanValue;
            }
            else if (_currentDialogue != null && _currentDialogue.Properties.Select(e => e.Name).Contains(name))
            {
                DialogueExposedPropertiesData property = _currentDialogue.Properties.First(e => e.Name == name);
                if (property.Type == "Bool")  return property.BooleanValue;
            }

            return false;
        }

        public void SetFlag(string name, bool value)
        {
            if (_exposedVariables.ContainsKey(name))
            {
                if (_exposedVariables[name].Type == "Bool") _exposedVariables[name].BooleanValue = value;
            }
            else
            {
                DialogueExposedPropertiesData property = new DialogueExposedPropertiesData();
                property.Name = name;
                property.BooleanValue = value;
                property.Type = "Bool";
                _exposedVariables.Add(name, property);
            }
        }

        public int? GetInt(string name)
        {
            if (_exposedVariables.ContainsKey(name))
            {
                if (_exposedVariables[name].Type == "Int") return _exposedVariables[name].IntValue;
            }
            else if (_currentDialogue != null && _currentDialogue.Properties.Select(e => e.Name).Contains(name))
            {
                DialogueExposedPropertiesData property = _currentDialogue.Properties.First(e => e.Name == name);
                if (property.Type == "Int") return property.IntValue;
            }

            return null;
        }

        public void SetInt(string name, int value)
        {
            if (_exposedVariables.ContainsKey(name))
            {
                if (_exposedVariables[name].Type == "Int")_exposedVariables[name].IntValue = value;
            }
            else
            {
                DialogueExposedPropertiesData property = new DialogueExposedPropertiesData();
                property.Name = name;
                property.IntValue = value;
                property.Type = "Int";
                _exposedVariables.Add(name, property);
            }
        }


        public virtual void StartDialogue(string dialogueName, System.Action finishCallback = null)
        {
            if (_dialogueGrapths.ContainsKey(dialogueName))
            {
                _completelyFinishedCallback?.Invoke();
                _completelyFinishedCallback = finishCallback;
                _currentDialogue = _dialogueGrapths[dialogueName];
                _isDialogueInProgress = true;
                _currentDialogueNode = _currentDialogue.Nodes.Find(n => n.Type == Enumeration.DialogueType.Start);
                _currentNodeType = _currentDialogueNode.Type;
                ProcessCurrentNode();
            }
            else
            {
                Debug.LogWarning($"'{dialogueName}' is an invaild Dialogue Graph name. Cannot start dialogue.");
            }
        }

        public void FinishDialogue()
        {
            _currentDialogue = null;
            _currentDialogueNode = null;
            _isDialogueInProgress = false;

            //DisabeCinematicCamera();

            OnDialogueFinished();
            var func = _completelyFinishedCallback;
            _completelyFinishedCallback = null;
            func?.Invoke();
        }

        protected void InitializeFlag()
        {
            if (_currentDialogueNode != null && _currentDialogueNode.PropertyType == "Bool")
            {
                string flagName = _currentDialogueNode.PropertyName;
                bool flag = _currentDialogueNode.PropertyBooleanValue;
                if (flagName != string.Empty && flagName != null)
                {
                    if (_exposedVariables.ContainsKey(flagName)) _exposedVariables[flagName].BooleanValue = flag;
                    else 
                    {
                        DialogueExposedPropertiesData property = new DialogueExposedPropertiesData();
                        property.Name = flagName;
                        property.BooleanValue = flag;
                        property.Type = "Bool";
                        _exposedVariables.Add(flagName, property);
                    }
                }
            }
        }

        protected string GetCurrentFlag()
        {
            string flag = _currentDialogueNode.PropertyName;
            return flag;
        }

        protected void NextNode(int choice)
        {
            if (_currentDialogueNode.Choices.Count == 0 || _currentDialogueNode.Choices.Count <= choice || _currentDialogueNode.Choices[choice].Next == string.Empty || _currentDialogueNode.Choices[choice].Next == null)
            {
                FinishDialogue();
                return;
            }

            _currentDialogueNode = _currentDialogue.Nodes.FirstOrDefault<DialogueNodeData>(n => _currentDialogueNode.Choices[choice].Next == n.Guid);

            if (_currentDialogueNode == null)
            {
                FinishDialogue();
                return;
            } 

            _currentNodeType = _currentDialogueNode.Type;
            ProcessCurrentNode();
        }

        private void IncrementVariable()
        {
            if (_currentDialogueNode != null && _currentDialogueNode.PropertyType == "Int")
            {
                string flagName = _currentDialogueNode.PropertyName;
                int value = GetInt(flagName) ?? 0;
                int incrementAmount = _currentDialogueNode.IncrementAmount;
                if (!string.IsNullOrEmpty(flagName))
                {
                    if (_exposedVariables.ContainsKey(flagName)) _exposedVariables[flagName].IntValue = value + incrementAmount;
                    else
                    {
                        DialogueExposedPropertiesData property = new DialogueExposedPropertiesData();
                        property.Name = flagName;
                        property.IntValue = value + incrementAmount;
                        property.Type = "Int";
                        _exposedVariables.Add(flagName, property);
                    }
                }
            }
        }

        private void SetInt()
        {
            if (_currentDialogueNode != null && _currentDialogueNode.PropertyType == "Int")
            {
                string flagName = _currentDialogueNode.PropertyName;
                int value = _currentDialogueNode.PropertyIntValue;
                if (!string.IsNullOrEmpty(flagName))
                {
                    if (_exposedVariables.ContainsKey(flagName)) _exposedVariables[flagName].IntValue = value;
                    else
                    {
                        DialogueExposedPropertiesData property = new DialogueExposedPropertiesData();
                        property.Name = flagName;
                        property.IntValue = value;
                        property.Type = "Int";
                        _exposedVariables.Add(flagName, property);
                    }
                }
            }
        }

        private int HandleComparator()
        {
            string flagName = _currentDialogueNode.PropertyName;
            int? hasValue = GetInt(flagName);
            if (hasValue == null) return 0;
            int value = hasValue.Value;
            ComparisonType comparisonType = _currentDialogueNode.ComparisonType;
            int condition = _currentDialogueNode.ComparsionCondition;

            switch (comparisonType)
            {
                case ComparisonType.Equals:
                    return (value == condition) ? 1 : 0;
                case ComparisonType.GreaterThan:
                    return (value > condition) ? 1 : 0;
                case ComparisonType.LessThan:
                    return (value < condition) ? 1 : 0;
            }

            return 0;
        } 

        private void EmitEvent(string eventName, int choice = -1)
        {
            if (!string.IsNullOrEmpty(eventName) && _events.TryGetValue(eventName, out UnityEvent<int> callback))
            {
                callback?.Invoke(choice);
            }
        }

        protected IEnumerator WaitThenNext(float timeToWait)
        {
            yield return new WaitForSeconds(timeToWait);
            NextNode(0);
        }

        protected virtual void EnableCinematicCamera()
        {
            if (_cinematicCamera && !_cinematicCamera.gameObject.activeSelf) _cinematicCamera.gameObject.SetActive(true);
        }

        protected virtual void DisabeCinematicCamera()
        {
            if (_cinematicCamera && _cinematicCamera.gameObject.activeSelf) _cinematicCamera.gameObject.SetActive(false);
        }

        protected virtual void MoveCinematicCamera(Transform loc, Transform lookAt, Transform parnet)
        {
            _cinematicCamera.transform.SetParent(loc);
            _cinematicCamera.transform.SetPositionAndRotation(loc.position, loc.rotation);
        }

        private Transform GetCameraLocation(string tag)
        {
            if (!_cameraLocations.TryGetValue(tag, out Transform cameraLoc))
            {
                if (!string.IsNullOrEmpty(tag))
                {
                    GameObject cameraLocGO = GameObject.FindGameObjectWithTag(tag);

                    if (cameraLocGO != null)
                    {
                        cameraLoc = cameraLocGO.transform;
                        _cameraLocations[tag] = cameraLoc;
                    }
                }
            }

            return cameraLoc;
        }

        protected virtual void TransitionCinematicCamera(Transform from, Transform to, Transform lookAt, float duration, Transform parent = null)
        {
            if (from == null || to == null) return;
            _cinematicCamera.transform.SetParent(parent);
            StartCoroutine(TransitionRoutine(from, to, lookAt, duration));
        }

        private IEnumerator TransitionRoutine(Transform from, Transform to, Transform lookAt, float duration)
        {
            float elapsed = 0f;

            from.GetPositionAndRotation(out Vector3 startPos, out Quaternion startRot);

            Quaternion endRot = to.rotation;
            if (lookAt != null)
            {
                Vector3 direction = (lookAt.position - to.position).normalized;
                endRot = Quaternion.LookRotation(direction);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float normalizedTime = elapsed / duration;

                _cinematicCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPos, to.position, normalizedTime), 
                    Quaternion.Slerp(startRot, endRot, normalizedTime)
                );
                yield return null;
            }

            _cinematicCamera.transform.SetPositionAndRotation(to.position, to.rotation);
            NextNode(0);
        }

        protected virtual void CinematicCameraLookAt(Transform lookAtLoc, float duration)
        {
            if (lookAtLoc == null) return;

            StartCoroutine(LookAtRoutine(lookAtLoc, duration));
        }

        private IEnumerator LookAtRoutine(Transform target, float duration)
        {
            float elapsed = 0f;

            Quaternion startRot = _cinematicCamera.transform.rotation;
            Vector3 direction = (target.position - _cinematicCamera.transform.position).normalized;
            Quaternion endRot = Quaternion.LookRotation(direction);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);

                    _cinematicCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, percent);
                }

                yield return null;
            }

            _cinematicCamera.transform.rotation = endRot;
            
            NextNode(0);
        }

        private void HandleCameraMove(string tag, string lookAtTag, float duration)
        {
            if (_cinematicCamera == null)
            {
                GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
                if (cinematicCameraGO != null)
                {
                    _cinematicCamera = cinematicCameraGO.GetComponent<Camera>();
                }

                if (!_cinematicCamera)
                {
                    Debug.LogWarning($"Cannot Move Dialogue Camera To '{tag}' As No Camera With tag '{_cinematicCameraTag}' Exist.");
                    NextNode(0);
                    return;
                }
            }

            if (!_cinematicCamera.gameObject.activeSelf)
            {
                NextNode(0);
                return;
            }

            Transform cameraLoc = GetCameraLocation(tag);
            if (cameraLoc == null)
            {
                Debug.LogWarning($"Cannot Move Dialogue Camera To '{tag}' As No GameObject With tag '{tag}' Exist.");
                NextNode(0);
                return;
            }

            Transform lookAtLoc = GetCameraLocation(lookAtTag);

            MoveCinematicCamera(cameraLoc, lookAtLoc, cameraLoc);
            StartCoroutine(WaitThenNext(duration));
        }

        private void HandleCameraTransition(string fromTag, string toTag, string lookAtTag, float duration)
        {
            if (_cinematicCamera == null)
            {
                GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
                if (cinematicCameraGO != null)
                {
                    _cinematicCamera = cinematicCameraGO.GetComponent<Camera>();
                }

                if (!_cinematicCamera)
                {
                    Debug.LogWarning($"Cannot Move Dialogue Camera To '{tag}' As No Camera With tag '{_cinematicCameraTag}' Exist.");
                    NextNode(0);
                    return;
                }
            }

            if (!_cinematicCamera.gameObject.activeSelf)
            {
                NextNode(0);
                return;
            }

            Transform fromLoc = GetCameraLocation(fromTag);
            if (fromLoc == null)
            {
                Debug.LogWarning($"Cannot Transition Dialogue Camera From '{fromTag}' to '{toTag}' As No GameObject With tag '{fromTag}' Exist.");
                NextNode(0);
                return;
            }

            Transform toLoc = GetCameraLocation(toTag);
            if (toLoc == null)
            {
                Debug.LogWarning($"Cannot Transition Dialogue Camera From '{fromTag}' to '{toTag}' As No GameObject With tag '{toTag}' Exist.");
                NextNode(0);
                return;
            }

            Transform lookAtLoc = GetCameraLocation(lookAtTag);

            TransitionCinematicCamera(fromLoc, toLoc, lookAtLoc, duration, toLoc);
        }

        protected void HandleCameraLookAt(string lookAtTag, float duration)
        {
            if (_cinematicCamera == null)
            {
                GameObject cinematicCameraGO = GameObject.FindGameObjectWithTag(_cinematicCameraTag);
                if (cinematicCameraGO != null)
                {
                    _cinematicCamera = cinematicCameraGO.GetComponent<Camera>();
                }

                if (!_cinematicCamera)
                {
                    Debug.LogWarning($"Dialogue Camera Cannot Look At '{lookAtTag}' As No Camera With tag '{_cinematicCameraTag}' Exist.");
                    NextNode(0);
                    return;
                }
            }

            if (!_cinematicCamera.gameObject.activeSelf)
            {
                NextNode(0);
                return;
            }

            Transform lookAtLoc = GetCameraLocation(lookAtTag);
            if (lookAtTag == null)
            {
                Debug.LogWarning($"Dialogue Camera Cannot Look At '{lookAtTag}' As No GameObject With tag '{lookAtTag}' Exist.");
                NextNode(0);
                return;
            }

            CinematicCameraLookAt(lookAtLoc, duration);
        }

        protected void ProcessCurrentNode()
        {
            switch (_currentNodeType)
            {
                case DialogueType.Start:
                case DialogueType.Connector:
                    NextNode(0);
                    break;
                case DialogueType.SingleChoice:
                case DialogueType.MultipleChoice:
                    PlayDialogueNode(_currentNodeType);
                    break;
                case DialogueType.SetBoolean:
                    InitializeFlag();
                    NextNode(0);
                    break;
                case DialogueType.Branch:
                    string flag = GetCurrentFlag();
                    NextNode(GetFlag(flag) ? 1 : 0);
                    break;
                case DialogueType.Increment:
                    IncrementVariable();
                    NextNode(0);
                    break;
                case DialogueType.Comparator:
                    NextNode(HandleComparator());
                    break;
                case DialogueType.SetInt:
                    SetInt();
                    NextNode(0);
                    break;
                case DialogueType.WaitForSeconds:
                    StartCoroutine(WaitThenNext(_currentDialogueNode.Duration));
                    break;
                case DialogueType.EmitEvent:
                    EmitEvent(_currentDialogueNode.EventID);
                    NextNode(0);
                    break;
                case DialogueType.EnableCinematicCamera:
                    EnableCinematicCamera();
                    NextNode(0);
                    break;
                case DialogueType.DisableCinematicCamera:
                    DisabeCinematicCamera();
                    NextNode(0);
                    break;
                case DialogueType.CameraMove:
                case DialogueType.CameraMoveFor:
                    HandleCameraMove(
                        _currentDialogueNode.CameraToLocationTag, 
                        _currentDialogueNode.CameraLookAtTag, 
                        _currentDialogueNode.Duration
                    );
                    break;
                case DialogueType.CameraTransition:
                    HandleCameraTransition(
                        _currentDialogueNode.CameraFromLocationTag, 
                        _currentDialogueNode.CameraToLocationTag,
                        _currentDialogueNode.CameraLookAtTag,
                        _currentDialogueNode.Duration
                    );
                    break;
                case DialogueType.CameraLookAt:
                    HandleCameraLookAt(
                        _currentDialogueNode.CameraLookAtTag, 
                        _currentDialogueNode.Duration
                    );
                    break;
            }
        }

        protected void InitializeDialogues()
        {
            _dialogueGrapths = new Dictionary<string, DialogueGraphSO>();
            if (!_db)
            {
                Debug.LogWarning("No Dialogue Database Set To Initialize.");
                return;
            }
            List<DialogueGraphSO> sos = _db.GetAllEntries();
            foreach (DialogueGraphSO so in sos)
            {
                _dialogueGrapths.Add(so.Name, so);
            }
        }

        public void OnDialogueNodeFinish(int choice)
        {
            if (_currentDialogueNode == null)
            {
                return;
            }
            EmitEvent(_currentDialogueNode.EventID, choice);
            NextNode(choice);
        }

        protected abstract void PlayDialogueNode(DialogueType type);
        public abstract void OnDialogueFinished();
    }
}