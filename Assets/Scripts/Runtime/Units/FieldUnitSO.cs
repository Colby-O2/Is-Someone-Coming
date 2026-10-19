using UnityEngine;

namespace ColbyO.SJ26.Dispatch
{
    [CreateAssetMenu(fileName = "DefaultUnitSO", menuName = "Sj26/Units/Unit")]
    public class FieldUnitSO : ScriptableObject
    {
        [Header("Idenity")]
        public string callsignSymbol = "P";
        public UnitType unitType = UnitType.Police;

        [Header("Icon")]
        public Texture2D icon;

        [Header("Movement")]
        public float moveSpeed = 2f;
        public float rotationSpeed = 8f;

        [Header("Patrol")]
        public bool autoPatrolWhenAvailable = true;
        public float patrolSpeed = 1f;
        public float idleWaitDuration = 3f;
    }
}
