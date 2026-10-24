using UnityEngine;

namespace ColbyO.SJ26
{
    [CreateAssetMenu(fileName = "DefaultMovementSettings", menuName = "SJ26/Player/MovementSettings")]
    public class MovementSettings : ScriptableObject
    {
        [Header("Look")]
        [Min(0)] public float ControllerSensitivityScaleFactor = 2f;
        [Min(0)] public float Sensitivity = 3f;
        public Vector2 YLookLimit = new Vector2(-80f, 80f);
        public bool InvertLookY = false;
        public bool InvertLookX = false;

        [Header("Movement")]
        [Min(0)] public float Speed = 10f;
        public float CrouchedMultiplier = 0.5f;
        [Range(0f, 1f)] public float BackwardSpeedMul = 0.5f;
        [Range(0f, 1f)] public float StrafingSpeedMul = 1f;
        [Min(0)] public float GravityMul = 1f;
        [Min(0)] public float InputSmoothing = 15f;
        [Range(0f, 1f)] public float AirControl = 0.2f;
        [Min(0)] public float TerminalVelocity = 50f;

        [Header("Heading Bobing")]
        public bool EnableHeadMotion = true;
        [Range(0.001f, 0.01f)] public float HeadBobAmount;
        [Range(1f, 30f)] public float HeadBobFreqency;
        [Range(10f, 100f)] public float HeadBobSmoothing;
        public float RunSpeed = 4.0f;
    }
}
