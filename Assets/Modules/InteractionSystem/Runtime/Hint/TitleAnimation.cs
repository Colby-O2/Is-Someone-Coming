using UnityEngine;

namespace InteractionSystem.Hint
{
    public class TitleAnimation : MonoBehaviour
    {
        [SerializeField] private float _radius = 1f;
        [SerializeField] private float _snapSpeed = 1f;
        [SerializeField] private float _angleStep = 10f;
        [SerializeField] private float _smoothTime = 0.08f;

        private float _currentAngle = 0f;
        private Vector3 _velocity;
        private Vector3 _startPos;

        private void Start()
        {
            _startPos = transform.localPosition;

            _currentAngle = Random.Range(0f, 360f);
        }

        private void Update()
        {
            _currentAngle += _snapSpeed * Time.deltaTime;
            float snappedAngle = Mathf.Round(_currentAngle / _angleStep) * _angleStep;
            float rad = snappedAngle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _radius;

            Vector3 target = _startPos + offset;

            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                target,
                ref _velocity,
                _smoothTime
            );
        }
    }
}
