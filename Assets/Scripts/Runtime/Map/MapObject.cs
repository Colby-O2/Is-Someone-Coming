using UnityEngine;

using System.Collections.Generic;

using PlazmaGames.Core;
using PlazmaGames.Attribute;
using PlazmaGames.Core.Debugging;

using ColbyO.SJ26.Street;

namespace ColbyO.SJ26.Map
{
    public abstract class MapObject : MonoBehaviour
    {
        protected static readonly List<MapObject> ActiveMapObjects = new List<MapObject>();

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Map Object Data")]
        [SerializeField] protected MapObjectSO _mapObjectData;

        [Header("Icon Display Settings")]
        [SerializeField] protected GameObject _iconPrefab;
        [SerializeField] protected Vector3 _iconOffset = new Vector3(0f, 2.5f, 0f);
        [SerializeField] protected Vector2 _iconSize = new Vector2(9f * 0.8571f, 9f);
        [SerializeField] protected bool _faceUpward = true;
        [SerializeField] protected float _stackDetectionRadius = 3.0f;
        [SerializeField] protected float _iconSpreadRadius = 2.0f;
        [SerializeField] protected float _iconOffsetSmoothSpeed = 10f;
        [SerializeField] protected float _renderOrderHeightStep = 0.05f;

        [Header("Map Positioning")]
        [SerializeField] protected bool _snapToRoadOnStart = true;
        [SerializeField] protected string _startingAddress = "";
        [SerializeField, ReadOnly] protected string _currentLocationLabel = "Unknown";

        protected GameObject _spawnedIcon;
        protected Renderer _iconRenderer;
        protected Renderer _selectionRenderer;
        protected MaterialPropertyBlock _propBlock;
        protected MaterialPropertyBlock _selectionPropBlock;

        protected Vector3 _currentVisualSpreadOffset = Vector3.zero;
        protected IStreetMonoSystem _streetSystem;

        public string CurrentLocation => _currentLocationLabel;
        public Vector3 VisualSpreadOffset => _currentVisualSpreadOffset;
        public int RenderOrder => _mapObjectData != null ? _mapObjectData.renderOrder : 0;

        public void SetSelected(bool value, Color? color = null)
        {
            if (_spawnedIcon == null || _spawnedIcon.transform.childCount == 0) return;

            Transform child = _spawnedIcon.transform.GetChild(0);
            if (child == null) return;

            child.gameObject.SetActive(value);

            if (color.HasValue)
            {
                if (_selectionRenderer == null)
                {
                    _selectionRenderer = child.GetComponent<Renderer>();
                }

                if (_selectionRenderer != null)
                {
                    _selectionPropBlock ??= new MaterialPropertyBlock();
                    _selectionRenderer.GetPropertyBlock(_selectionPropBlock);
                    _selectionPropBlock.SetColor(BaseColorId, color.Value);
                    _selectionRenderer.SetPropertyBlock(_selectionPropBlock);
                }
            }
        }

        public void SetIconColor(Color color)
        {
            if (_spawnedIcon == null) return;

            if (_iconRenderer == null)
            {
                _iconRenderer = _spawnedIcon.GetComponent<Renderer>();
            }

            if (_iconRenderer != null)
            {
                _propBlock ??= new MaterialPropertyBlock();
                _iconRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor(BaseColorId, color);
                _iconRenderer.SetPropertyBlock(_propBlock);
            }
        }

        protected virtual void OnEnable()
        {
            if (!ActiveMapObjects.Contains(this))
            {
                ActiveMapObjects.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            ActiveMapObjects.Remove(this);
        }

        protected virtual void Start()
        {
            if (_iconPrefab == null)
            {
                _iconPrefab = Resources.Load<GameObject>("Prefabs/MapObjectIconPrefab");
            }

            _streetSystem = GameManager.GetMonoSystem<IStreetMonoSystem>();

            if (_snapToRoadOnStart && _streetSystem != null)
            {
                SnapToAddressOrNearestRoad(_startingAddress);
            }

            SetupIcon();
            UpdateCurrentLocationLabel();
        }

        protected virtual void Update()
        {
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
            UpdateIconSpreadOffset();
        }

        public virtual void SetupIcon()
        {
            if (_mapObjectData == null) return;

            Texture targetTexture = _mapObjectData.icon;
            if (targetTexture == null)
            {
                PlazmaDebug.LogWarning($"No valid texture found on MapObjectSO: {_mapObjectData.name}.", $"MapObject {name}");
                return;
            }

            if (_spawnedIcon == null)
            {
                if (_iconPrefab == null)
                {
                    _iconPrefab = Resources.Load<GameObject>("Prefabs/MapObjectIconPrefab");
                }

                if (_iconPrefab != null)
                {
                    _spawnedIcon = Instantiate(_iconPrefab, transform);
                    _iconRenderer = _spawnedIcon.GetComponent<Renderer>();
                    if (_spawnedIcon.transform.childCount > 0)
                    {
                        _selectionRenderer = _spawnedIcon.transform.GetChild(0).GetComponent<Renderer>();
                    }
                }
            }

            if (_spawnedIcon == null) return;

            _spawnedIcon.transform.position = transform.position + GetEffectiveIconOffset();
            _spawnedIcon.transform.localScale = new Vector3(_iconSize.x, _iconSize.y, 1f);

            float xRot = _faceUpward ? 90f : 0f;
            _spawnedIcon.transform.rotation = Quaternion.Euler(xRot, 90f, 0f);

            ApplyRenderOrder(RenderOrder);
            ApplyIconTexture(targetTexture, _mapObjectData.color);
        }

        public void ApplyRenderOrder(int order)
        {
            if (_spawnedIcon == null) return;

            if (_iconRenderer == null)
            {
                _iconRenderer = _spawnedIcon.GetComponent<Renderer>();
            }

            if (_iconRenderer != null)
            {
                _iconRenderer.sortingOrder = order;
            }

            if (_selectionRenderer == null && _spawnedIcon.transform.childCount > 0)
            {
                _selectionRenderer = _spawnedIcon.transform.GetChild(0).GetComponent<Renderer>();
            }

            if (_selectionRenderer != null)
            {
                _selectionRenderer.sortingOrder = order - 1;
            }
        }

        public void ApplyIconTexture(Texture targetTexture, Color color)
        {
            Debug.Log("111111");
            if (_spawnedIcon == null || targetTexture == null) return;

            Debug.Log("222222");
            if (_iconRenderer == null)
            {
                _iconRenderer = _spawnedIcon.GetComponent<Renderer>();
            }

            Debug.Log("3333333");
            if (_iconRenderer != null)
            {
                _propBlock ??= new MaterialPropertyBlock();
                _iconRenderer.GetPropertyBlock(_propBlock);
                _propBlock.SetTexture(BaseMapId, targetTexture);
                _propBlock.SetTexture(MainTexId, targetTexture);
                _propBlock.SetColor(BaseColorId, color);
                _iconRenderer.SetPropertyBlock(_propBlock);
                Debug.Log("444444");
            }
        }

        protected Vector3 GetEffectiveIconOffset()
        {
            return _iconOffset + new Vector3(0f, RenderOrder * _renderOrderHeightStep, 0f);
        }

        protected virtual void UpdateIconSpreadOffset()
        {
            if (_spawnedIcon == null) return;

            Vector3 targetSpreadOffset = Vector3.zero;

            if (ActiveMapObjects.Count > 1)
            {
                int totalOverlapping = 0;
                int myOverlapIndex = 0;

                for (int i = 0; i < ActiveMapObjects.Count; i++)
                {
                    MapObject other = ActiveMapObjects[i];
                    if (other == null) continue;

                    if (other == this)
                    {
                        myOverlapIndex = totalOverlapping;
                        totalOverlapping++;
                    }
                    else if (AreObjectsStacked(other))
                    {
                        totalOverlapping++;
                    }
                }

                if (totalOverlapping > 1)
                {
                    float angleRad = (myOverlapIndex / (float)totalOverlapping) * Mathf.PI * 2f;
                    float dynamicRadius = _iconSpreadRadius * (1f + Mathf.Max(0, totalOverlapping - 2) * 0.2f);
                    targetSpreadOffset = new Vector3(
                        Mathf.Cos(angleRad) * dynamicRadius,
                        myOverlapIndex * 0.05f,
                        Mathf.Sin(angleRad) * dynamicRadius
                    );
                }
            }

            _currentVisualSpreadOffset = Vector3.Lerp(
                _currentVisualSpreadOffset,
                targetSpreadOffset,
                _iconOffsetSmoothSpeed * Time.deltaTime
            );

            _spawnedIcon.transform.position = transform.position + GetEffectiveIconOffset() + _currentVisualSpreadOffset;

            float xRot = _faceUpward ? 90f : 0f;
            _spawnedIcon.transform.rotation = Quaternion.Euler(xRot, 90f, 0f);
        }

        protected virtual bool AreObjectsStacked(MapObject other)
        {
            Vector2 myPos2D = new Vector2(transform.position.x, transform.position.z);
            Vector2 otherPos2D = new Vector2(other.transform.position.x, other.transform.position.z);

            return Vector2.Distance(myPos2D, otherPos2D) <= _stackDetectionRadius;
        }

        public virtual void SnapToAddressOrNearestRoad(string targetAddress = "")
        {
            _streetSystem ??= GameManager.GetMonoSystem<IStreetMonoSystem>();
            if (_streetSystem == null) return;

            var streets = _streetSystem.GetNamedStreets();
            if (streets == null || streets.Count == 0) return;

            if (!string.IsNullOrEmpty(targetAddress))
            {
                foreach (var street in streets)
                {
                    foreach (var addr in street.addresses)
                    {
                        if (addr.addressLabel.Equals(targetAddress, System.StringComparison.OrdinalIgnoreCase) ||
                            addr.addressLabel.Contains(targetAddress))
                        {
                            transform.position = addr.position;
                            UpdateCurrentLocationLabel();
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
            UpdateCurrentLocationLabel();
        }

        protected virtual void UpdateCurrentLocationLabel()
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
    }
}