using UnityEngine;

namespace ColbyO.SJ26.Utilities
{
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private float wobbleSpeed = 1.5f;
        [SerializeField] private float wobbleAmount = 0.05f;
        [SerializeField] private float rotationAmount = 1f;

        private Vector3 _startPos;
        private Quaternion _startRot;

        public bool IsPaused { get; set; }

        private void Start()
        {
            IsPaused = false;
            ResetDefaultState();
        }

        private void Update()
        {
            if (IsPaused) return;

            float wobbleX = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmount;
            float wobbleY = Mathf.Cos(Time.time * wobbleSpeed * 0.8f) * wobbleAmount;

            transform.localPosition = _startPos + new Vector3(0, wobbleX, wobbleY);

            transform.localRotation = _startRot * Quaternion.Euler(
                Mathf.Sin(Time.time * wobbleSpeed) * rotationAmount,
                Mathf.Cos(Time.time * wobbleSpeed * 0.6f) * rotationAmount,
                0
            );
        }

        public void ResetDefaultState()
        {
            _startPos = transform.localPosition;
            _startRot = transform.localRotation;
        }
    }
}