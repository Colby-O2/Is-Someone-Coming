using PlazmaGames.Attribute;
using PlazmaGames.Math;
using UnityEngine;

namespace ColbyO.SJ26.Utilities
{
    public class VelocityTracker : MonoBehaviour
    {
        [SerializeField] private float _dt = 0.1f;
        [SerializeField] private float _smoothing;

        public Vector3 Velocity { get; private set; }
        public float Speed => Velocity.magnitude;
        public float SpeedInPlane => Velocity.SetY(0).magnitude;

        [SerializeField, ReadOnly] private Vector3 _lastPosition;
        [SerializeField, ReadOnly] private Vector3 _smoothVel;
        [SerializeField, ReadOnly] private float _timer;

        private void Awake()
        {
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            _timer += Time.fixedDeltaTime;

            if (_timer >= _dt)
            {
                Vector3 curPos = transform.position;
                Vector3 rawVel = (curPos - _lastPosition) / _timer;
                _smoothVel = Vector3.Lerp(_smoothVel, rawVel, 1f - Mathf.Exp(-_timer / _smoothing));
                Velocity = _smoothVel;

                _lastPosition = curPos;
                _timer = 0f;
            }
        }

    }
}