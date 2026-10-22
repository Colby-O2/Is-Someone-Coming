using System.Collections.Generic;
using ColbyO.SJ26.Dispatch;
using ColbyO.SJ26.Map;
using InteractionSystem.Interfaces;
using InteractionSystem.UI;
using PlazmaGames.Core;
using PlazmaGames.Math;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ColbyO.SJ26
{
    public class DispatchViewScreen : MonoBehaviour, IInspectorClickable
    {
        [SerializeField] private List<UiCastObject> _rects = new();

        private UICascadeCaster _ucc;

        private void Start()
        {
            _ucc = new UICascadeCaster(_rects);
        }
            
        public void OnClick()
        {
            Vector2 p = VirtualCaster.GetVirtualMousePosition() / (VirtualCaster.Instance._canvas.transform as RectTransform).rect.size;
            Debug.Log(_ucc.Resolve(p));
            if (_ucc.Raycast(p)?.transform is Transform hit && hit.parent?.TryGetComponent<MapObject>(out var mo) != null)
            {
                GameManager.GetMonoSystem<IDispatchMonoSystem>().HandleMapObjectClick(mo);
            }
        }

        public void OnDrag()
        {
        }

        public void OnHoverEnter()
        {
        }

        public void OnHover()
        {
        }

        public void OnHoverExit()
        {
        }

        public void OnRelease()
        {
        }
    }
}
