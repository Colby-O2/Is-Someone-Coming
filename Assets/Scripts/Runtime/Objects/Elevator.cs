using UnityEngine;

using PlazmaGames.Core;
using PlazmaGames.Animation;

using TMPro;

namespace ColbyO.SJ26
{
    public class Elevator : MonoBehaviour
    {
        [SerializeField] private Transform _doorL;
        [SerializeField] private Transform _doorR;
        [SerializeField] private GameObject _blockage;

        [SerializeField] private TMP_Text _floorNumberText;

        [SerializeField] private float _defaultDuration;
        [SerializeField] private float _slideDistance;

        [SerializeField] private AudioSource _as;
        [SerializeField] private AudioClip _openClip;
        [SerializeField] private AudioClip _closeClip;
        [SerializeField] private AudioClip _movingClip;
        [SerializeField] private AudioClip _stopingClip;

        private Vector3 _doorLClosedPos;
        private Vector3 _doorRClosedPos;
        private Vector3 _doorLOpenPos;
        private Vector3 _doorROpenPos;

        private Promise _doorPromise;
        private Promise _movingPromise;

        private int _doorState= 0;

        private void Awake()
        {
            if (_doorL != null)
            {
                _doorLClosedPos = _doorL.localPosition;
                _doorLOpenPos = _doorLClosedPos + new Vector3(0f, 0f, -_slideDistance);
            }

            if (_doorR != null)
            {
                _doorRClosedPos = _doorR.localPosition;
                _doorROpenPos = _doorRClosedPos + new Vector3(0f, 0f, _slideDistance);
            }
        }

        private void MoveDoors(Vector3 targetPosL, Vector3 targetPosR, float duration, int targetState)
        {
            _doorState = -1;

            Vector3 startPosL = (_doorL != null) ? _doorL.localPosition : Vector3.zero;
            Vector3 startPosR = (_doorR != null) ? _doorR.localPosition : Vector3.zero;

            GameManager.GetMonoSystem<IAnimationMonoSystem>().RequestAnimation(this, duration, (float t) =>
            {
                if (_doorL != null) _doorL.localPosition = Vector3.Lerp(startPosL, targetPosL, t);
                if (_doorR != null) _doorR.localPosition = Vector3.Lerp(startPosR, targetPosR, t);
            })
            .Then(_ =>
            {
                _doorState = targetState;

                Promise.ResolveExisting(ref _doorPromise);
            });
        }

        public Promise Open(float duration = -1f)
        {
            float animDuration = (duration < 0f) ? (_openClip != null ? _openClip.length : _defaultDuration) : duration;

            if (_doorState == -1)
            {
                return null;
            }

            _blockage.SetActive(false);

            _doorPromise = null;
            _doorPromise = Promise.CreateExisting(ref _doorPromise);

            if (_as != null && _openClip != null)
            {
                _as.Stop();
                _as.PlayOneShot(_openClip);
            }

            MoveDoors(_doorLOpenPos, _doorROpenPos, animDuration, 1);

            return _doorPromise;
        }

        public Promise Close(float duration = -1f)
        {
            float animDuration = (duration < 0f) ? (_closeClip != null ? _closeClip.length : _defaultDuration) : duration;

            if (_doorState == 0)
            {
                return null;
            }

            _blockage.SetActive(true);

            _doorPromise = null;
            _doorPromise = Promise.CreateExisting(ref _doorPromise);

            if (_as != null && _closeClip != null)
            {
                _as.Stop();
                _as.PlayOneShot(_closeClip);
            }

            MoveDoors(_doorLClosedPos, _doorRClosedPos, animDuration, 0);

            return _doorPromise;
        }

        public Promise StartMoving()
        {
            if (_as != null && _movingClip != null)
            {
                _as.Stop();
                _as.PlayOneShot(_movingClip);
                return SJ26GameManager.GlobalScheduler.Wait(_movingClip.length);
            }

            return null;
        }

        public Promise StopMoving(float duration = -1)
        {
            if (_as != null && _stopingClip != null)
            {
                _as.Stop();
                _as.PlayOneShot(_stopingClip);
                return SJ26GameManager.GlobalScheduler.Wait((duration < 0f) ? _stopingClip.length : duration);
            }

            return null;
        }
    }
}
