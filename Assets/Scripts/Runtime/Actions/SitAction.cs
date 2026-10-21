using UnityEngine;

using PlazmaGames.Animation;
using PlazmaGames.Attribute;
using PlazmaGames.Core;

using InteractionSystem;
using InteractionSystem.Actions;

namespace ColbyO.SJ26
{
    [System.Serializable]
    public class SitAction : InteractionAction
    {
        [SerializeField] private Transform _target;
        [SerializeField, ReadOnly] private bool _isPlayerSitting = false;

        public override string ActionName => "Sit";

        public override bool CanExecute() => !_isPlayerSitting && !SJ26GameManager.PlayerSit.HasDetachedHead() && SJ26GameManager.PlayerAnimator.CanSit();

        public override void Execute(InteractorController interactor)
        {
           SitController player = SJ26GameManager.PlayerSit;
            if (player && !player.HasDetachedHead() && SJ26GameManager.PlayerAnimator.CanSit())
            {
                Debug.Log(SJ26GameManager.PlayerAnimator.GetState());
                _isPlayerSitting = true;

                Transform playerTrans = player.transform;

                Vector3 directionAwayFromChair = playerTrans.position - _target.position;
                directionAwayFromChair.y = 0;
                Quaternion endRot = Quaternion.LookRotation(directionAwayFromChair);

                Quaternion startRot = playerTrans.rotation;

                GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(
                    _owner,
                    0.5f,
                    (float t) =>
                    {
                        RotateStep(t, playerTrans, startRot, endRot);
                    }
                ).Then(_ =>
                {
                    player.DetachHead(_target)
                    .Then(_ =>
                    {
                        _isPlayerSitting = false;
                    });
                });
            }
        }

        public Transform GetSitTarget() => _target;
        private void RotateStep(float t, Transform playerTrans, Quaternion startRot, Quaternion endRot)
        {
            playerTrans.rotation = Quaternion.Slerp(startRot, endRot, t);
        }
    }
}