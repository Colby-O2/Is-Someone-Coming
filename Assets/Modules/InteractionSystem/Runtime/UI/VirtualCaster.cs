using InteractionSystem.Attribute;
using InteractionSystem.Helpers;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace InteractionSystem.UI
{
    public class VirtualCaster : GraphicRaycaster
    {
        [SerializeField] private Camera _screenCamera;
        [SerializeField] private RectTransform _virtualCursor;
        [SerializeField] private Canvas _canvas;
        private RectTransform _canvasRect;
        [SerializeField, ReadOnly] private Vector2 _cursorPos;

        public static VirtualCaster Instance { get; private set; }

        public static UnityEvent OnMouseDrag = new();

        public static void ShowCursor() => Instance._virtualCursor.gameObject.SetActive(true);

        public static void HideCursor()
        {
            if (!Instance) return;
            Instance._cursorPos = Vector2.zero;
            Instance._virtualCursor.anchoredPosition = Instance._cursorPos;
            Instance._virtualCursor.gameObject.SetActive(false);
        }

        public static Vector2 GetVirtualMousePosition()
        {
            if (!Instance._virtualCursor) return Vector2.zero;
            return RectTransformUtility.WorldToScreenPoint(
                Instance._canvas.worldCamera,
                Instance._virtualCursor.position
            );
        }

        public static void WrapCursorPosition(Vector2 delta)
        {
            if (!Instance._virtualCursor || !Instance._virtualCursor.gameObject.activeSelf) return;
            
            if (!Instance._canvasRect) Instance._canvasRect = Instance._virtualCursor.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            
            float halfWidth = Instance._canvasRect.rect.width * 0.5f;
            float halfHeight = Instance._canvasRect.rect.height * 0.5f;

            Instance._cursorPos = Instance._cursorPos
                .SetX(Mathf.Clamp(Instance._cursorPos.x + delta.x, -halfWidth, halfWidth))
                .SetY(Mathf.Clamp(Instance._cursorPos.y + delta.y, -halfHeight, halfHeight));

            Instance._virtualCursor.anchoredPosition = Instance._cursorPos;

            OnMouseDrag?.Invoke();
        }

        private bool ScreenSpaceCast<T>(Vector3 pos, out T output, float maxDst = Mathf.Infinity, int layer = ~0)
        {
            output = default;
            Ray ray = _screenCamera.ScreenPointToRay(pos);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDst, layer))
            {
                if (hit.collider != null)
                {
                    if (hit.collider.TryGetComponent<T>(out output))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool Raycast<T>(Vector3 mousePosition, out T output, float maxDst = Mathf.Infinity, int layer = ~0)
        {
            output = default;

            Ray ray = eventCamera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~LayerMask.NameToLayer("UI")))
            {
                if (hit.collider.transform == transform)
                {
                    Vector3 virtualPos = new Vector3(hit.textureCoord.x, hit.textureCoord.y);
                    virtualPos.x *= _screenCamera.targetTexture.width;
                    virtualPos.y *= _screenCamera.targetTexture.height;

                    return ScreenSpaceCast<T>(virtualPos, out output, maxDst, layer);
                }
            }

            return false;
        }

        private bool ScreenSpaceCast(Vector3 pos, out RaycastHit hit, float maxDst = Mathf.Infinity, int layer = ~0)
        {
            hit = default;
            Ray ray = _screenCamera.ScreenPointToRay(pos);
            bool hasHit = Physics.Raycast(ray, out hit, maxDst, layer);
            return hasHit;
        }

        public bool Raycast(Vector3 mousePosition, out RaycastHit hit, float maxDst = Mathf.Infinity, int layer = ~0)
        {
            hit = default;
            Ray ray = eventCamera.ScreenPointToRay(mousePosition);
            if (Physics.Raycast(ray, out RaycastHit _hit, Mathf.Infinity, ~LayerMask.NameToLayer("UI")))
            {
                if (_hit.collider.transform == transform)
                {
                    Vector3 virtualPos = new Vector3(_hit.textureCoord.x, _hit.textureCoord.y);
                    virtualPos.x *= _screenCamera.targetTexture.width;
                    virtualPos.y *= _screenCamera.targetTexture.height;

                    return ScreenSpaceCast(virtualPos, out hit, maxDst, layer);
                }
            }

            return false;
        }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            _canvasRect = _canvas.GetComponent<RectTransform>();
            HideCursor();
        }
    }
}