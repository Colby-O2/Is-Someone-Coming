using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    PSXEffectFeature.cs
//-----------------------------------------------------------------------
namespace ColbyO.VolumetricLights
{
    public sealed class VolumetricLightingFeature : ScriptableRendererFeature
    {
        [Header("Settings")]
        [SerializeField] private RenderPassEvent _renderEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        private Shader _psxEffectShader;

        private Material _material;
        private VolumetricLightingPass _volumetricLightingPass;

        public override void Create()
        {
            if (_psxEffectShader == null)
                _psxEffectShader = Shader.Find("Hidden/VolumetricLighting");

            if (_material == null)
                _material = CoreUtils.CreateEngineMaterial(_psxEffectShader);

            _volumetricLightingPass ??= new VolumetricLightingPass(_material)
            {
                renderPassEvent = _renderEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null || _volumetricLightingPass == null)
            {
                Debug.LogWarning("Volumetric Lighting missing material or pass.");
                return;
            }

            VolumeStack stack = VolumeManager.instance.stack;
            VolumetricLightingSettings settings = stack.GetComponent<VolumetricLightingSettings>();

            bool isGameCamera = renderingData.cameraData.cameraType == CameraType.Game;
            bool isSceneView = renderingData.cameraData.cameraType == CameraType.SceneView && settings.ShowInSceneView.value;

            if (
                settings != null &&
                settings.IsActive() &&
                (isGameCamera || isSceneView)
            )
            {
                _volumetricLightingPass.Setup(_material);
                renderer.EnqueuePass(_volumetricLightingPass);
            }
        }
    }
}