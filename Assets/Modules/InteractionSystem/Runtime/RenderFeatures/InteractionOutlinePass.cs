using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace InteractionSystem.Rendering
{
    public sealed class InteractionOutlinePass : ScriptableRenderPass
    {
        private Material _mat;

        public void Setup(Material mat)
        {
            _mat = mat;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            if (_mat == null) return;

            if (cameraData.cameraType == CameraType.Preview) return;

            RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            TextureHandle tempTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_OutlineTempTex", true);
            TextureHandle activeColor = resourceData.activeColorTexture;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("OutlinePostProcess", out var passData))
            {
                passData.Source = activeColor;
                passData.Destination = tempTex;
                passData.OutlineMaterial = _mat;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.Destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                    float width = desc.width;
                    float height = desc.height;
                    data.OutlineMaterial.SetVector("_TexelSize", new Vector4(1f / width, 1f / height, width, height));

                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), data.OutlineMaterial, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("OutlineBackToCamera", out var passData))
            {
                passData.Source = tempTex;
                passData.Destination = activeColor;

                builder.UseTexture(passData.Source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.Destination, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        private class PassData
        {
            public TextureHandle Source;
            public TextureHandle Destination;
            public Material OutlineMaterial;
        }
    }
}
