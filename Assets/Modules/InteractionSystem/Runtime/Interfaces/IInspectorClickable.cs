using UnityEngine;

namespace InteractionSystem.Interfaces
{
    public interface IInspectorClickable
    {
        public void OnClick();
        public void OnDrag();
        public void OnHoverEnter();
        public void OnHover();
        public void OnHoverExit();
        public void OnRelease();
    }
}
