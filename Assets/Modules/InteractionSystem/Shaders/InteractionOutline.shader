Shader "InteractionSystem/OutlineShader"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color", Color) = (0,0,0,1) 
        _Thickness("Thickness", Float) = 4.5
        _MaskThreshold("Mask Threshold", Float) = 1
        _NormalThreshold("Normal Threshold", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Overlay" }
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            SAMPLER(sampler_BlitTexture);

            TEXTURE2D_X(_InteractionNormalsTex);
            SAMPLER(sampler_InteractionNormalsTex);

            TEXTURE2D_X(_InteractionMaskTex);
            SAMPLER(sampler_InteractionMaskTex);

            TEXTURE2D_X(_InteractionDepthTex);
            SAMPLER(sampler_InteractionDepthTex);

            float4 _OutlineColor;
            float _Thickness;
            float _MaskThreshold;
            float _NormalThreshold;
            float _EdgeStrength;
            float _EdgeThreshold;

            float4 _TexelSize;

            float SobelNormal(float2 uv, float2 texel)
            {
                float3 n00 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(-1,-1)).xyz;
                float3 n10 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(0,-1)).xyz;
                float3 n20 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(1,-1)).xyz;
                float3 n01 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(-1,0)).xyz;
                float3 n21 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(1,0)).xyz;
                float3 n02 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(-1,1)).xyz;
                float3 n12 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(0,1)).xyz;
                float3 n22 = SAMPLE_TEXTURE2D_X(_InteractionNormalsTex, sampler_InteractionNormalsTex, uv + texel * float2(1,1)).xyz;

                float3 gx = n20 + 2*n21 + n22 - (n00 + 2*n01 + n02);
                float3 gy = n02 + 2*n12 + n22 - (n00 + 2*n10 + n20);

                return sqrt(dot(gx, gx) + dot(gy, gy));
            }

            float SobelMask(float2 uv, float2 texel)
            {
                float d00 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(-1,-1)).r;
                float d10 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(0,-1)).r;
                float d20 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(1,-1)).r;
                float d01 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(-1,0)).r;
                float d21 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(1,0)).r;
                float d02 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(-1,1)).r;
                float d12 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(0,1)).r;
                float d22 = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv + texel * float2(1,1)).r;

                float gx = d20 + 2*d21 + d22 - (d00 + 2*d01 + d02);
                float gy = d02 + 2*d12 + d22 - (d00 + 2*d10 + d20);

                return sqrt(gx*gx + gy*gy);
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 texel = _Thickness / _ScreenParams.xy;

                float rawSceneDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
                float sceneDepth = LinearEyeDepth(rawSceneDepth, _ProjectionParams);

                float rawMaskDepth = SAMPLE_TEXTURE2D_X(_InteractionDepthTex, sampler_InteractionDepthTex, uv).r;
                float maskDepth = LinearEyeDepth(rawMaskDepth, _ProjectionParams);

                float4 maskSample = SAMPLE_TEXTURE2D_X(_InteractionMaskTex, sampler_InteractionMaskTex, uv);
                float mask = maskSample.r;

                if (sceneDepth < maskDepth)
                {
                    mask = 0;
                }

                float maskEdge = SobelMask(uv, texel);
                float normalEdge = SobelNormal(uv, texel);
    
                float edge = saturate((maskEdge * _MaskThreshold) + (normalEdge * _NormalThreshold));
                float edgeStrength = edge * mask;

                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                float outline = smoothstep(0.5, 1.0, edgeStrength);
                return lerp(sceneColor, _OutlineColor, outline);
            }

            ENDHLSL
        }
    }
}