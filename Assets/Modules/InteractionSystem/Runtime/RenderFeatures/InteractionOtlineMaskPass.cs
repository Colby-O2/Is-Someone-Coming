using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace InteractionSystem
{
    public sealed class InteractionOutlineMaskPass : ScriptableRenderPass
    {
        static readonly ShaderTagId _shaderTagLit = new ShaderTagId("UniversalForward");
        static readonly ShaderTagId _shaderTagUnlit = new ShaderTagId("SRPDefaultUnlit");
        static readonly ShaderTagId _shaderTagForwardOnly = new ShaderTagId("UniversalForwardOnly");

        private Material _maskMaterial;
        private LayerMask _mask;

        private bool _hasNormalPass;

        public InteractionOutlineMaskPass(LayerMask mask)
        {
            _mask = mask;
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public void UpdateMaskMaterial(Material maskMaterial)
        {

            if (maskMaterial != null && maskMaterial != _maskMaterial)
            {
                _maskMaterial = maskMaterial;
                _hasNormalPass = _maskMaterial.GetTag("InteractionNormalPass", true, "Disabled").CompareTo("Enabled") == 0;
            }
        }

        public void SetLayerMask(LayerMask mask)
        {
            _mask = mask;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();

            if (cameraData.cameraType == CameraType.Preview) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.colorFormat = RenderTextureFormat.R8;
            desc.depthBufferBits = 0;
            TextureHandle maskTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_InteractionMaskTex", true);

            TextureHandle interactionNormalsTex;
            if (_hasNormalPass)
            {
                RenderTextureDescriptor normalDesc = cameraData.cameraTargetDescriptor;
                normalDesc.colorFormat = RenderTextureFormat.ARGB32;
                normalDesc.depthBufferBits = 0;
                interactionNormalsTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, normalDesc, "_InteractionNormalsTex", true);
            }
            else
            {
                interactionNormalsTex = resourceData.cameraNormalsTexture;
            }

                RenderTextureDescriptor depthDesc = cameraData.cameraTargetDescriptor;
            depthDesc.colorFormat = RenderTextureFormat.Depth;
            depthDesc.depthBufferBits = 32;
            TextureHandle interactionDepthTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthDesc, "_InteractionDepthTex", true);

            SortingSettings sortingSettings = new SortingSettings(cameraData.camera) { criteria = SortingCriteria.CommonOpaque };
            DrawingSettings drawingSettings = new DrawingSettings(_shaderTagLit, sortingSettings)
            {
                overrideMaterial = _maskMaterial,
                //enableDynamicBatching = universalRenderingData.supportsDynamicBatching,
                perObjectData = universalRenderingData.perObjectData
            };

            drawingSettings.SetShaderPassName(1, _shaderTagUnlit);
            drawingSettings.SetShaderPassName(2, _shaderTagForwardOnly);

            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all, _mask);

            RendererListParams listParams = new RendererListParams(universalRenderingData.cullResults, drawingSettings, filteringSettings);
            RendererListHandle listHandle = renderGraph.CreateRendererList(listParams);

            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("InteractionOutlineMaskPass", out var passData))
            {
                passData.MaskTexture = maskTex;
                passData.NormalsTexture = interactionNormalsTex;
                passData.DepthTexture = interactionDepthTex;
                passData.RendererList = listHandle;

                builder.SetRenderAttachment(passData.MaskTexture, 0, AccessFlags.Write);
                if (_hasNormalPass) builder.SetRenderAttachment(passData.NormalsTexture, 1, AccessFlags.Write);
                else builder.UseTexture(passData.NormalsTexture, AccessFlags.Read);
                builder.SetRenderAttachmentDepth(passData.DepthTexture, AccessFlags.Write);

                builder.UseRendererList(passData.RendererList);

                builder.SetGlobalTextureAfterPass(passData.MaskTexture, Shader.PropertyToID("_InteractionMaskTex"));
                builder.SetGlobalTextureAfterPass(passData.NormalsTexture, Shader.PropertyToID("_InteractionNormalsTex"));
                builder.SetGlobalTextureAfterPass(passData.DepthTexture, Shader.PropertyToID("_InteractionDepthTex"));

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(false, true, Color.clear);
                    context.cmd.DrawRendererList(data.RendererList);
                });
            }
        }

        private class PassData
        {
            public TextureHandle MaskTexture;
            public TextureHandle NormalsTexture;
            public TextureHandle DepthTexture;
            public RendererListHandle RendererList;
        }
    }
}