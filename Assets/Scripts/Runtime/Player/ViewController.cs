using UnityEngine;

using PlazmaGames.Attribute;
using PlazmaGames.Core;
using PlazmaGames.Math;

using ColbyO.SJ26.Input;

namespace ColbyO.SJ26
{
    public class ViewController : MonoBehaviour
    {
        [SerializeField] private MovementSettings _settings;
        [SerializeField] private Transform _playerBody;

        [SerializeField, ReadOnly] private Vector3 _startHeadPos;
        [SerializeField, ReadOnly] private Vector3 _headPos;
        [SerializeField, ReadOnly] private float _pitch = 0f;
        [SerializeField, ReadOnly] private float _yaw = 0f;

        private IInputMonoSystem _input;

        public float Sensitivity => _settings.Sensitivity;
        public bool IsHeadBobbingEnabled => _settings.EnableHeadMotion;

        private void Awake()
        {
            _startHeadPos = transform.localPosition;
            _headPos = _startHeadPos;
            if (!_playerBody) _playerBody = transform.parent;
            _input = GameManager.GetMonoSystem<IInputMonoSystem>();
        }

        private void Update()
        {
            if (SJ26GameManager.LockMovement || SJ26GameManager.IsPaused) return;
            UpdateRotation();
        }

        private void StartHeadBob()
        {
            Vector3 pos = Vector3.zero;
            pos.x += Mathf.Lerp(pos.x, Mathf.Sin(Time.time * _settings.HeadBobFreqency) * _settings.HeadBobAmount * 1.4f, _settings.HeadBobSmoothing * Time.deltaTime);
            pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * _settings.HeadBobFreqency) * _settings.HeadBobAmount * 1.4f, _settings.HeadBobSmoothing * Time.deltaTime);
            transform.localPosition += pos;
        }

        private void CheckForHeadMovement()
        {
            float movementAmount = new Vector3(_input.RawMovement.x, 0f, _input.RawMovement.y).magnitude;
            if (movementAmount > 0f)
            {
                StartHeadBob();
            }
        }

        private void StopHeadMovement()
        {
            if (transform.localPosition == _headPos) return;
            transform.localPosition = Vector3.Lerp(transform.localPosition, _headPos, Time.deltaTime);
        }

        private void ProcessHead()
        {
            _pitch = transform.localEulerAngles.AngleAs180().x;
            _pitch -= (_settings.InvertLookY ? -1 : 1) * Sensitivity * _input.RawLook.y;
            _pitch = Mathf.Clamp(_pitch, _settings.YLookLimit.x, _settings.YLookLimit.y);
            transform.localEulerAngles = transform.localEulerAngles.SetX(_pitch);
        }

        private void ProcessBody()
        {
            _yaw = _playerBody.transform.localEulerAngles.y;
            _yaw += (_settings.InvertLookX ? -1 : 1) * Sensitivity * _input.RawLook.x;
            _playerBody.transform.localEulerAngles = _playerBody.transform.localEulerAngles.SetY(_yaw);
        }

        private void UpdateRotation()
        {
            ProcessHead();
            ProcessBody();

            if (IsHeadBobbingEnabled)
            {
                CheckForHeadMovement();
                StopHeadMovement();
            }
        }
    }
}