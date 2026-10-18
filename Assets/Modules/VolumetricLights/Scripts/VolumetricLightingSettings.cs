using UnityEngine;
using UnityEngine.Rendering;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    
//-----------------------------------------------------------------------
namespace ColbyO.VolumetricLights
{
    [System.Serializable, VolumeComponentMenu("Lighting/Volumetric Lighting Settings")]
    public class VolumetricLightingSettings : VolumeComponent, IPostProcessComponent
    {
        [Header("Settings")]
        public BoolParameter Enabled = new BoolParameter(true);
        public BoolParameter ShowInSceneView = new BoolParameter(false);

        public ClampedIntParameter MarchingSteps = new ClampedIntParameter(32, 1, 256);
        public ClampedFloatParameter NoiseOffset = new ClampedFloatParameter(1f, 0f, 1000f);

        [Header("Main Light")]
        public ClampedFloatParameter MainLightIntensity = new ClampedFloatParameter(1f, 0f, 1000f);
        public ClampedFloatParameter MainLightScattering = new ClampedFloatParameter(0f, -1f, 1f);

        [Header("Additional Light")]
        public ClampedFloatParameter AdditionalLightIntensity = new ClampedFloatParameter(1f, 0f, 1000f);
        public ClampedFloatParameter AdditionalLightScattering = new ClampedFloatParameter(0f, -1f, 1f);

        [Header("Fog")]
        public ClampedFloatParameter BaseDensity = new ClampedFloatParameter(0.05f, 0f, 1f);
        public ClampedFloatParameter FogScale = new ClampedFloatParameter(1f, 0f, 100f);
        public ClampedFloatParameter FogSpeed = new ClampedFloatParameter(1f, 0f, 10f);

        public bool IsActive() => Enabled.value;
        public bool IsTileCompatible() => false;
    }
}