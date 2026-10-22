using UnityEngine;

using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using PlazmaGames.Math;
using PlazmaGames.Core.Debugging;

namespace ColbyO.SJ26
{

    [System.Serializable]
    public class UiCastObject
    {
        public GameObject Object;
        [CanBeNull] public Camera Camera;
    }
    
    [System.Serializable]
    public class UICascadeCaster
    {
        [SerializeField] private List<UiCastObject> _layers;
        
        public UICascadeCaster(List<UiCastObject> layers)
        {
            _layers = layers;
        }
        
        private static Rect? getLocalRect(RectTransform rect, Canvas canvas)
        {
            Rect? canvasRect = (canvas.transform.GetChild(0) as RectTransform)?.rect;
            if (canvasRect == null) return null;
            Rect r = (rect.parent as RectTransform)!.GetWorldRect();
            Bounds b = RectTransformUtility.CalculateRelativeRectTransformBounds(canvas.transform as RectTransform, rect);
            Rect rr = new Rect(b.min.x, b.min.y, b.max.x - b.min.x, b.max.y - b.min.y);
            Rect c = new Rect(canvasRect.Value.width / 2, canvasRect.Value.height / 2, canvasRect.Value.width, canvasRect.Value.height);
            return new Rect(
                (rr.x + c.x) / c.width,
                (rr.y + c.y) / c.height,
                rr.width / c.width,
                rr.height / c.height);
        }

        public Vector2? Resolve(Vector2 pos)
        {
            foreach (UiCastObject layer in _layers)
            {
                if (layer.Object.TryGetComponent<RectTransform>(out var rt))
                {
                    Canvas canvas = rt.GetComponentInParent<Canvas>();
                    if (canvas == null) return null;
                    Rect? r = getLocalRect(rt, canvas);
                    if (r == null) return null;
                    pos.x = (pos.x - r.Value.x) / r.Value.width;
                    pos.y = (pos.y - r.Value.y) / r.Value.height;
                }
                else
                {
                    if (!layer.Camera)
                    {
                        PlazmaDebug.LogError("World object needs to have a camera attached!", "UiCascadeCaster");
                        return null;
                    }
                    if (!Physics.Raycast(layer.Camera.ViewportPointToRay(pos), out var hit, 4, 1 << layer.Object.layer))
                    {
                        return null;
                    }
                    pos = layer.Object.transform.InverseTransformPoint(hit.point).XZ() + new Vector2(0.5f, 0.5f);
                }
            }

            return pos;
        }

        public RaycastHit? Raycast(Vector2 pos, int layerMask = ~0, float maxDistance = float.PositiveInfinity)
        {
            Camera output = _layers.Last().Camera;
            if (!output)
            {
                PlazmaDebug.LogError("No output attached!", "UiCascadeCaster");
                return null;
            }
            Vector2? res = Resolve(pos);
            if (res == null) return null;
            if (Physics.Raycast(output.ViewportPointToRay(res.Value), out var hit, maxDistance, layerMask))
            {
                return hit;
            }

            return null;
        }
    }
}
