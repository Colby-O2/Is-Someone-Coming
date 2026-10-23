using System.Collections.Generic;
using ColbyO.SJ26.Map;
using PlazmaGames.Core.MonoSystem;

namespace ColbyO.SJ26.Dispatch
{
    public interface IDispatchMonoSystem : IMonoSystem
    {
        void RegisterUnit(FieldUnit unit);
        void UnregisterUnit(FieldUnit unit);
        bool DispatchUnitToAddress(string unitCallsign, string targetAddress, string callId = "");
        bool DispatchUnitToAddress(FieldUnit unit, string targetAddress, string callId = "");
        IReadOnlyList<FieldUnit> GetAllUnits();
        FieldUnit GetUnitByCallsign(string callsign);
        int GetNumberOfRegisteredUnits(UnitType? unitType = null, string callsignSymbol = null);
        void HandleMapObjectClick(MapObject mo);
        void StartTheCalls();
        void StopTheCalls();
        void AnswerPhone();
        void HideAllUnits();
        void UnHideAllUnits();
        public void StartCallWith(DispatchEventSO eventSO);
        void MakeFailActiveEvents();
        void RemoveAllEvents();
        bool PhoneIsRinging();
        ActiveDispatchEvent InCall();
    }
}