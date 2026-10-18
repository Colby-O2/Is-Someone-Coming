using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

//-----------------------------------------------------------------------
// Author:  Colby-O
// File:    
//-----------------------------------------------------------------------
namespace ColbyO.VolumetricLights
{
    public class VolumetricLightingPass : ScriptableRenderPass
    {
        private const string _passName = "PSXEffectPass";
        private Material _material;

        public VolumetricLightingPass(Material mat)
        {
            _material = mat;
            requiresIntermediateTexture = true;
        }

        public void Setup(Material material)
        {
            _material = material;
        }

        private void UpdateMaterialWithSettings(Material material, VolumetricLightingSettings settings)
        {
            material.SetInt("_StepCount", settings.MarchingSteps.value);
            material.SetFloat("_NoiseOffset", settings.NoiseOffset.value);

            material.SetFloat("_MainLightIntensity", settings.MainLightIntensity.value);
            material.SetFloat("_AdditionalLightIntensity", settings.AdditionalLightIntensity.value);

            material.SetFloat("_MainLightScattering", settings.MainLightScattering.value);
            material.SetFloat("_AdditionalLightScattering", settings.AdditionalLightScattering.value);

            Texture2D fogTex = Resources.Load<Texture2D>("Fog");
            material.SetTexture("_FogTex", fogTex);
            material.SetFloat("_FogScale", settings.FogScale.value);
            material.SetFloat("_FogSpeed", settings.FogSpeed.value);
            material.SetFloat("_BaseDensity", settings.BaseDensity.value);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            VolumeStack stack = VolumeManager.instance.stack;
            VolumetricLightingSettings settings = stack.GetComponent<VolumetricLightingSettings>();
            if (settings == null || !settings.IsActive()) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            cameraData.requiresDepthTexture = true;

            RenderTextureDescriptor cameraDesc = cameraData.cameraTargetDescriptor;

            TextureDesc fogDesc = new TextureDesc(cameraDesc);
            fogDesc.colorFormat = cameraDesc.graphicsFormat;

            float scale = 0.25f;
            fogDesc.width = Mathf.Max(1, Mathf.RoundToInt(cameraDesc.width * scale));
            fogDesc.height = Mathf.Max(1, Mathf.RoundToInt(cameraDesc.height * scale));

            fogDesc.depthBufferBits = 0;
            fogDesc.name = "VolumetricFogBuffer";

            TextureHandle fogBuffer = renderGraph.CreateTexture(fogDesc);

            fogDesc.name = "BlurredVolumetricFog";
            TextureHandle blurredFogBuffer = renderGraph.CreateTexture(fogDesc);

            TextureHandle src = resourceData.activeColorTexture;

            TextureDesc dstDesc = renderGraph.GetTextureDesc(src);
            dstDesc.name = _passName;
            TextureHandle dst = renderGraph.CreateTexture(dstDesc);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Volumetric Raymarch", out PassData passData))
            {
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                passData.src = src;
                passData.material = _material;
                passData.settings = settings;
                passData.depthTex = resourceData.activeDepthTexture;

                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.UseTexture(passData.depthTex, AccessFlags.Read);

                if (resourceData.mainShadowsTexture.IsValid())
                    builder.UseTexture(resourceData.mainShadowsTexture, AccessFlags.Read);
                if (resourceData.additionalShadowsTexture.IsValid())
                    builder.UseTexture(resourceData.additionalShadowsTexture, AccessFlags.Read);

                builder.SetRenderAttachment(fogBuffer, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    data.material.SetTexture("_CameraDepthTexture", data.depthTex);
                    UpdateMaterialWithSettings(data.material, data.settings);
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass("Volumetric Composite", out PassData passData))
            {
                passData.material = _material;
                passData.src = fogBuffer; 

                builder.UseTexture(fogBuffer, AccessFlags.Read);
                builder.UseTexture(src, AccessFlags.Read);
                builder.SetRenderAttachment(dst, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Vector4 texelSize = new Vector4(1f / fogDesc.width, 1f / fogDesc.height, fogDesc.width, fogDesc.height);
                    data.material.SetVector("_FogBuffer_TexelSize", texelSize);
                    data.material.SetTexture("_FogBuffer", data.src);
                    data.material.SetTexture("_SceneTexture", src);

                    Blitter.BlitTexture(context.cmd, new Vector4(1, 1, 0, 0), data.material, 1);
                });
            }

            resourceData.cameraColor = dst;
        }

        private class PassData
        {
            public TextureHandle src;
            public TextureHandle depthTex;
            public Material material;
            public VolumetricLightingSettings settings;
        }
    }
}