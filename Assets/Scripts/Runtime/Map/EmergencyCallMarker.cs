using ColbyO.SJ26.Dispatch;
using UnityEngine;

namespace ColbyO.SJ26.Map
{
    public class EmergencyCallMarker : MapObject
    {
        [SerializeField] private string _callId = "#1047";

        [Header("Expiring Settings")]
        [SerializeField] private float _warningThresholdRatio = 0.35f;
        [SerializeField] private float _warningThresholdSeconds = 10f;
        [SerializeField] private float _minPulseSpeed = 4f;
        [SerializeField] private float _maxPulseSpeed = 14f;

        private bool _isEnded = false;
        private bool _isSuccess = false;
        private bool _isResolving = false;
        private bool _isWarningActive = false;
        private float _pulsePhase = 0f;

        private ActiveDispatchEvent _finishedEvent = null;

        public string CallId() => _callId;

        public bool IsEnded() => _isEnded;
        public bool IsSuccess() => IsEnded() && _isSuccess;

        public ActiveDispatchEvent FinishedEvent() => _finishedEvent;

        public void InitializeCall(MapObjectSO callIconData, string callId, string targetAddress)
        {
            _mapObjectData = callIconData;
            _callId = callId;
            _startingAddress = targetAddress;

            SnapToAddressOrNearestRoad(targetAddress);
            SetupIcon();
        }

        public void UpdateTimerVisuals(float timeLeft, float totalTime)
        {
            if (_isEnded || _isResolving || totalTime <= 0f) return;

            float warningWindow = Mathf.Max(totalTime * _warningThresholdRatio, _warningThresholdSeconds);

            if (timeLeft <= warningWindow)
            {
                _isWarningActive = true;

                float urgency = 1f - Mathf.Clamp01(timeLeft / warningWindow);
                float currentSpeed = Mathf.Lerp(_minPulseSpeed, _maxPulseSpeed, urgency);

                _pulsePhase += Time.deltaTime * currentSpeed;
                float t = (Mathf.Sin(_pulsePhase) + 1f) * 0.5f;

                Color flashColor = Color.Lerp(Color.white, Color.red, t);
                SetIconColor(flashColor);
                SetSelected(t > 0.5f, Color.red);
            }
            else if (_isWarningActive)
            {
                ClearWarningVisuals();
            }
        }

        public void SetResolving(bool isResolving)
        {
            _isResolving = isResolving;
            _isWarningActive = false;
            _pulsePhase = 0f;

            if (isResolving)
            {
                SetSelected(true, Color.green);
                SetIconColor(Color.green);
            }
            else
            {
                SetSelected(false, Color.white);
                SetIconColor(Color.white);
            }
        }

        private void ClearWarningVisuals()
        {
            _isWarningActive = false;
            _pulsePhase = 0f;
            SetSelected(false, Color.white);
            SetIconColor(Color.white);
        }

        public void SetSuccess(ActiveDispatchEvent e)
        {
            _finishedEvent = e;
            _isEnded = true;
            _isSuccess = true;
            _isResolving = false;
            ClearWarningVisuals();
            ApplyIconTexture(SJ26GameManager.Preferences.SuccessMapIcon, Color.white);
        }

        public void SetFailed(ActiveDispatchEvent e)
        {
            _finishedEvent = e;
            _isEnded = true;
            _isSuccess = false;
            _isResolving = false;
            ClearWarningVisuals();
            ApplyIconTexture(SJ26GameManager.Preferences.FailedMapIcon, Color.white);
        }
    }
}