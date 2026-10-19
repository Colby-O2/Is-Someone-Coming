using PlazmaGames.Core.MonoSystem;
using System.Collections.Generic;
using UnityEngine;

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
    }
}