using UnityEngine;
using UnityEngine.UIElements;

namespace InteractionSystem.Settings
{
    [System.Serializable]
    internal class MoveableSettings
    {
        public enum RotateType
        {
            DoNot,
            JustYAxis,
            Full,
        }
        public float HoldDistance = 0.4f;
        public RotateType RotationType = RotateType.JustYAxis;
        public bool IgnoreChildren;

        [HideInInspector] public SphereCollider Bounds;

        public float BoundingRadius() => this.Bounds.radius;
    }
}