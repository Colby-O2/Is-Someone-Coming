using UnityEngine;

using ColbyO.SJ26.Map;

namespace ColbyO.SJ26.Dispatch
{
    [CreateAssetMenu(fileName = "DefaultUnitSO", menuName = "SJ26/Map/Unit")]
    public class FieldUnitSO : MapObjectSO
    {
        [Header("Identity")]
        public string callsignSymbol = "P";
        public UnitType unitType = UnitType.Police;

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float rotationSpeed = 8f;

        [Header("Patrol")]
        public bool autoPatrolWhenAvailable = true;
        public float patrolSpeed = 1f;
        public float idleWaitDuration = 3f;
    }
}