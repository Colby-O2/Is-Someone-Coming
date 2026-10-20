using UnityEngine;

namespace ColbyO.SJ26.Map
{
    public class EmergencyCallMarker : MapObject
    {
        [SerializeField] private string _callId = "#1047";

        public void InitializeCall(MapObjectSO callIconData, string callId, string targetAddress)
        {
            _mapObjectData = callIconData;
            _callId = callId;
            _startingAddress = targetAddress;

            SnapToAddressOrNearestRoad(targetAddress);
            SetupIcon();
        }
    }
}