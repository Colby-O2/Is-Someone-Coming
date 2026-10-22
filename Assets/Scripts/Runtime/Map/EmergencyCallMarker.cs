using ColbyO.SJ26.Dispatch;
using UnityEngine;

namespace ColbyO.SJ26.Map
{
    public class EmergencyCallMarker : MapObject
    {
        [SerializeField] private string _callId = "#1047";

        private bool _isEnded = false;
        private bool _isSuccess = false;

        public bool IsEnded() => _isEnded;
        public bool IsSuccess() => IsEnded() && _isSuccess;

        public void InitializeCall(MapObjectSO callIconData, string callId, string targetAddress)
        {
            _mapObjectData = callIconData;
            _callId = callId;
            _startingAddress = targetAddress;

            SnapToAddressOrNearestRoad(targetAddress);
            SetupIcon();
        }
        
        public void SetSuccess(DispatchEvent e)
        {
            _isEnded = true;
            _isSuccess = true;
            ApplyIconTexture(SJ26GameManager.Preferences.SuccessMapIcon, Color.white);
        }

        public void SetFailed(DispatchEvent e)
        {
            _isEnded = true;
            ApplyIconTexture(SJ26GameManager.Preferences.FailedMapIcon, Color.white);
        }
    }
}