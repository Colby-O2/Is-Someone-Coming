using UnityEngine;

using PlazmaGames.Core;
using PlazmaGames.UI;

using InteractionSystem;
using InteractionSystem.UI;

namespace ColbyO.SJ26.UI
{
    public class InspectionView : View
    {
        [SerializeField] private InspectionUIController _controller;

        [SerializeField] private AudioSource _as;
        [SerializeField] private AudioClip _pickupClip;
        [SerializeField] private AudioClip _dropClip;

        public override void Init()
        {
            _controller.OnShow.AddListener(() =>
            {
                if (_as && _pickupClip) _as.PlayOneShot(_pickupClip);
                GameManager.GetMonoSystem<IUIMonoSystem>().Show<InspectionView>();
            });
            _controller.OnHide.AddListener(() =>
            {
                if (!SJ26GameManager.PlayerInteractor.GetInspectorHandler().HasUI)
                {
                    VirtualCaster.HideCursor();
                    return;
                }
                if (_as && _dropClip) _as.PlayOneShot(_dropClip);
                GameManager.GetMonoSystem<IUIMonoSystem>().ShowLast();
            });
        }

        public override void Show()
        {
            base.Show();
            VirtualCaster.ShowCursor();
        }

        public override void Hide()
        {
            base.Hide();
            VirtualCaster.HideCursor();
        }
    }
}
