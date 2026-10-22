using ColbyO.SJ26.Dispatch;
using UnityEngine;

namespace ColbyO.SJ26.Map
{
    public class EmergencyCallMarker : MapObject
    {
        [SerializeField] private string _callId = "#1047";

        private bool _isEnded = false;
        private bool _isSuccess = false;
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
        
        public void SetSuccess(ActiveDispatchEvent e)
        {
            _finishedEvent = e;
            _isEnded = true;
            _isSuccess = true;
            ApplyIconTexture(SJ26GameManager.Preferences.SuccessMapIcon, Color.white);
        }

        public void SetFailed(ActiveDispatchEvent e)
        {
            _finishedEvent = e;
            _isEnded = true;
            ApplyIconTexture(SJ26GameManager.Preferences.FailedMapIcon, Color.white);
        }
    }
}