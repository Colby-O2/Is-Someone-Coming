using InteractionSystem.Actions;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem.Hint
{
    public sealed class InteractionHint : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text _name;

        [SerializeField] private GameObject _actionHolder;
        [SerializeField] private ActionUI[] _actions = new ActionUI[4];

        [SerializeField] private float _lineWidth = 0.05f;
        [SerializeField] private float _outlineWidth = 0.1f;
        [SerializeField] private LineRenderer _hintLine;
        [SerializeField] private LineRenderer _outlineLine;
        [SerializeField] private Transform _hintDot;

        [SerializeField] private Color _enabledColor = Color.white;
        [SerializeField] private Color _disabledColor = Color.gray;

        [Header("Outline")]
        [SerializeField] private string _outlineLayer;

        [Header("Line Style")]
        [SerializeField] private int _minPoints = 3;
        [SerializeField] private int _maxPoints = 20;
        [SerializeField] private float _curveHeight = -0.5f;
        [SerializeField] private float _resolutionSpeed = 5f;

        [Header("Hint Dot Jiggle")]
        [SerializeField] private float _jiggleAmount = 0.05f;
        [SerializeField] private float _snapSpeed = 5f;
        [SerializeField] private float _snapInterval = 0.2f;

        [Header("Fade by Distance")]
        [SerializeField] private float _fadeStartDistance = 4f;   
        [SerializeField] private float _fadeEndDistance = 1.5f;    
        [SerializeField] private float _fadeSpeed = 6f;

        [Header("Dynamic Scaling")]
        [SerializeField] private bool _useDynamicScaling = true;
        [SerializeField] private float _baseDistance = 2f;
        [SerializeField] private float _minScale = 0.5f;
        [SerializeField] private float _maxScale = 3f;

        private float _currentAlpha = 0f;
        private Transform _player;
        private Transform _playerCamera;
        private Interactable _owner;

        private float _currentOffset = 0f;
        private float _targetOffset = 0f;
        private float _snapTimer = 0f;

        private Vector3 _start;
        private Vector3 _end;
        private float _time;

        private int _unusedActions = 0;

        private Dictionary<Transform, int> originalLayers;
        private bool _layerIsSet = false;

        private void Awake()
        {
            _hintLine.useWorldSpace = true;

            _hintLine.startWidth = _lineWidth;
            _hintLine.endWidth = _lineWidth;

            _outlineLine.startWidth = _lineWidth + _outlineWidth;
            _outlineLine.endWidth = _lineWidth + _outlineWidth;

            ShowName();
            HideAction();
            Disable();
        }

        private void OnEnable()
        {
            _time = 0f;
            _snapTimer = 0f;
            _currentOffset = 0f;
            _targetOffset = 0f;

            UpdateFade();
            UpdateScale();
            UpdateControls();
            UpdateCurve();
            UpdateDot();
            UpdateOutline();
        }

        private void LateUpdate()
        {
            UpdateFade();
            UpdateScale();
            UpdateControls();
            UpdateCurve();
            UpdateDot();
            UpdateOutline();
        }

        public void SetOwner(Interactable owner) => _owner = owner;

        public void SetPlayer(Transform player) => _player = player;

        public void SetPlayerCamera(Transform cam) => _playerCamera = cam;

        public bool AreAnyActionsActive() => !(_unusedActions == _actions.Length);

        private void UpdateOutline()
        {
            var count = _hintLine.positionCount;

            Vector3[] pts = new Vector3[count];
            _hintLine.GetPositions(pts);

            _outlineLine.positionCount = count;
            _outlineLine.SetPositions(pts);
        }

        public void Enable()
        {
            bool anyActionActive = AreAnyActionsActive();
            if (!gameObject.activeSelf && anyActionActive) gameObject.SetActive(true);
            else if (gameObject.activeSelf && !anyActionActive) Disable();
        }

        public void Disable()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
            _hintDot.position = _end;
            HideName();
            HideAction();
        }

        public void ShowName()
        {
            if (!_name.gameObject.activeSelf) _name.gameObject.SetActive(true);
        }

        public void HideName()
        {
            if (_name.gameObject.activeSelf) _name.gameObject.SetActive(false);
        }

        private void StoreAndSet(Transform t, int newLayer)
        {
            if (t == transform) return;

            originalLayers[t] = t.gameObject.layer;

            t.gameObject.layer = newLayer;

            for (int i = 0; i < t.childCount; i++)
                StoreAndSet(t.GetChild(i), newLayer);
        }

        private void SetLayer(LayerMask layer)
        {
            if (_layerIsSet) return;

            _layerIsSet = true;

            if (originalLayers == null)
                originalLayers = new Dictionary<Transform, int>();
            else
                originalLayers.Clear();

            StoreAndSet(_owner.transform, layer);
        }

        private void RestoreLayer()
        {
            _layerIsSet = false;

            if (originalLayers == null)
                return;

            foreach (var entry in originalLayers)
            {
                if (entry.Key != null) entry.Key.gameObject.layer = entry.Value;
            }

            originalLayers.Clear();
        }

        public void ShowAction()
        {
            if (!AreAnyActionsActive())
            {
                HideAction();
                return;
            }
            if (!_actionHolder.activeSelf) _actionHolder.SetActive(true);
            SetLayer(LayerMask.NameToLayer(_outlineLayer));
        }

        public void HideAction()
        {
            if (_actionHolder.activeSelf) _actionHolder.SetActive(false);
            RestoreLayer();
        }

        private void MoveHint(Vector3 position)
        {
            _start = _hintLine.transform.position;
            _end = position;
        }

        public void MoveHint(Transform from)
        {
            Vector3 forward = Vector3.Normalize(from.position - _owner.transform.position);
            Vector3 right = Vector3.Cross(forward, from.up);
            SphereCollider br = _owner.BoundingRadius();
            Vector3 position = br.transform.TransformPoint(br.center);
            float radius = br.radius * br.transform.lossyScale.x;
            transform.SetPositionAndRotation(position + (from.up + right).normalized * radius, from.rotation);
            MoveHint(position);
        }

        public void Set(string name, InteractionAction[] actions)
        {
            _name.text = name;

            _unusedActions = 0;

            for (int i = 0; i < actions.Length; i++)
            {
                if (actions.Length <= i || actions[i] == null || !actions[i].CanExecute())
                {
                    _actions[i].SetColor(_disabledColor);
                    if (_actions[i].IsActive()) _actions[i].SetActive(false);
                    _unusedActions++;
                    continue;
                }

                if (!_actions[i].IsActive()) _actions[i].SetActive(true);
                _actions[i].SetColor(_enabledColor);
                _actions[i].Action = actions[i].ActionName;
            }
        }

        private void UpdateFade()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(_player.position, _end);
            float targetAlpha = Mathf.InverseLerp(_fadeStartDistance, _fadeEndDistance, dist);
            float newAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, Time.deltaTime * _fadeSpeed);
            _currentAlpha = newAlpha;

            Color c = _name.color;
            c.a = newAlpha;
            _name.color = c;

            if (_hintLine != null)
            {
                Color lc = _hintLine.material.color;
                lc.a = newAlpha;
                _hintLine.material.color = lc;
            }

            if (_outlineLine != null)
            {
                Color lc = _outlineLine.material.color;
                lc.a = newAlpha;
                _outlineLine.material.color = lc;
            }

            var rl = _hintDot.GetComponentsInChildren<Renderer>();
            if (rl != null)
            {
                foreach (var r in rl)
                {
                    Color dc = r.material.color;
                    dc.a = newAlpha;
                    r.material.color = dc;
                }
            }
        }

        private void UpdateScale()
        {
            if (!_useDynamicScaling || _playerCamera == null) return;

            float dist = Vector3.Distance(_playerCamera.position, transform.position);

            float scaleFactor = dist / _baseDistance;
            scaleFactor = Mathf.Clamp(scaleFactor, _minScale, _maxScale);

            transform.localScale = Vector3.one * scaleFactor;

            if (_hintLine != null)
            {
                _hintLine.startWidth = _lineWidth * scaleFactor;
                _hintLine.endWidth = _lineWidth * scaleFactor;
            }

            if (_outlineLine != null)
            {
                _outlineLine.startWidth = (_lineWidth + _outlineWidth) * scaleFactor;
                _outlineLine.endWidth = (_lineWidth + _outlineWidth) * scaleFactor;
            }
        }

        private void UpdateDot()
        {
            Vector3 mid = (_start + _end) * 0.5f;
            mid += Vector3.down * _curveHeight;

            float dotU = 1f;
            Vector3 tangent = 2 * (1 - dotU) * (mid - _start) + 2 * dotU * (_end - mid);
            tangent.Normalize();

            Vector3 curvePoint = Mathf.Pow(1 - dotU, 2) * _start
                               + 2 * (1 - dotU) * dotU * mid
                               + Mathf.Pow(dotU, 2) * _end;

            _snapTimer += Time.deltaTime;
            if (_snapTimer >= _snapInterval)
            {
                _snapTimer = 0f;
                _targetOffset = Random.Range(-_jiggleAmount, _jiggleAmount);
            }

            _currentOffset = Mathf.MoveTowards(_currentOffset, _targetOffset, _snapSpeed * Time.deltaTime);

            _hintDot.position = curvePoint + tangent * _currentOffset;
        }

        private void UpdateCurve()
        {
            if (!gameObject.activeSelf) return;

            _time += Time.deltaTime * _resolutionSpeed;
            float t = (Mathf.Sin(_time) + 1f) / 2f;
            int resolution = Mathf.RoundToInt(Mathf.Lerp(_minPoints, _maxPoints, t));
            _hintLine.positionCount = resolution;

            Vector3 mid = (_start + _end) * 0.5f;
            mid += Vector3.down * _curveHeight;

            for (int i = 0; i < resolution; i++)
            {
                float u = i / (float)(resolution - 1);
                Vector3 point = Mathf.Pow(1 - u, 2) * _start
                              + 2 * (1 - u) * u * mid
                              + Mathf.Pow(u, 2) * _end;
                _hintLine.SetPosition(i, point);
            }
        }

        private void UpdateControls()
        {
            if (_actionHolder.activeSelf)
            {
                foreach (ActionUI action in _actions)
                {
                    action.UpdateIconMaterial();
                    action.SetColor(action.IsActive() ? _enabledColor : _disabledColor);
                }
            }
        }
    }
}