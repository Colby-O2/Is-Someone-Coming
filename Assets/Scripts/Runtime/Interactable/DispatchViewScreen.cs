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
            Vector2 screenPosition = VirtualCaster.GetVirtualMousePosition();

            Vector2 p = new Vector2(
                screenPosition.x / Screen.width,
                screenPosition.y / Screen.height
            );

            if (_ucc.Raycast(p)?.transform is Transform hit &&
                hit.parent?.TryGetComponent<MapObject>(out var mo) == true)
            {
                GameManager.GetMonoSystem<IDispatchMonoSystem>()
                    .HandleMapObjectClick(mo);
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
