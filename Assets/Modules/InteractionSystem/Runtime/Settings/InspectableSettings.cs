using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace InteractionSystem.Settings
{
    internal enum InspectableLookType
    {
        Pickup,
        LookAt,
    }

    [System.Serializable]
    internal class InspectSubComponent
    {
        public Transform Transform;
        public string ReadTextOverride;
    }

    [System.Serializable]
    internal class InspectableSettings
    {
        public string Title = string.Empty;
        public InspectableLookType LookType;
        public Transform LookAtTarget;
        [Min(0f)] public float OffsetDistance;
        public bool AllowZoom;
        [Min(0f)] public float ZoomMin;
        public bool RotatePickup;
        public Vector3 PickupRotation;
        public string ReadText;
        public bool AllowRotate;
        public bool HasInteractions;
        public List<InspectSubComponent> ItemList;
        [HideInInspector] public SphereCollider Bounds;

        public float BoundingRadius() => this.Bounds.radius;

        public InspectableSettings CreateCopy()
        {
            return new InspectableSettings()
            {
                Title = this.Title,
                LookType = this.LookType,
                LookAtTarget = this.LookAtTarget,
                OffsetDistance = this.OffsetDistance,
                AllowZoom = this.AllowZoom,
                ZoomMin = this.ZoomMin,
                RotatePickup = this.RotatePickup,
                PickupRotation = this.PickupRotation,
                ReadText = this.ReadText,
                AllowRotate = this.AllowRotate,
                HasInteractions = this.HasInteractions,
                Bounds = this.Bounds,
            };
        }
    }
}
