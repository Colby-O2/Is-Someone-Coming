Shader "Hidden/VolumetricLighting"
{
    Properties {
        _StepCount("Steps", Int) = 32
        _Intensity("Intensity", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP

            #define MAX_STEP_COUNT 256

            int _StepCount;
            float _MainLightIntensity;
            float _AdditionalLightIntensity;
            float _MainLightScattering;
            float _AdditionalLightScattering;
            float _NoiseOffset;

            float _FogScale;
            float _FogSpeed;
            float _BaseDensity;

            sampler2D _FogTex;

            /**
            * Generates a random value from a 3D vector.
            *
            * @param p  The input 3D coordinates.
            * @return   A random float value in the range [0.0, 1.0].
            */
            inline float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            /**
            * Calculates a andom gradient vector for a given 3D point.
            *
            * @param p  The input 3D coordinates.
            * @return   A gradient direction.
            */
            inline float3 Gradient(float3 p)
            {
                float h = Hash31(p) * 6.2831853;
                return float3(cos(h), sin(h), cos(h * 0.5));
            }

            /**
            * Computes the quintic smoothing for Perlin noise.
            *
            * @param t  The distance [0, 1] within a cell.
            * @return   The smoothed weight.
            */
            inline float3 Fade(float3 t)
            {
                return t * t * t * (t * (t * 6 - 15) + 10);
            }

            /**
            * Generates 3D Perlin Noise for a given coordinate.
            *
            * @param p  The input 3D coordinates.
            * @return   A noise value in the range [0.0, 1.0].
            */
            inline float Perlin3D(float3 p)
            {
                float3 pi = floor(p);
                float3 pf = frac(p);

                float3 f = Fade(pf);

                // Calculate dot products between corner gradients and distance vectors
                float n000 = dot(Gradient(pi + float3(0,0,0)), pf - float3(0,0,0));
                float n100 = dot(Gradient(pi + float3(1,0,0)), pf - float3(1,0,0));
                float n010 = dot(Gradient(pi + float3(0,1,0)), pf - float3(0,1,0));
                float n110 = dot(Gradient(pi + float3(1,1,0)), pf - float3(1,1,0));
                float n001 = dot(Gradient(pi + float3(0,0,1)), pf - float3(0,0,1));
                float n101 = dot(Gradient(pi + float3(1,0,1)), pf - float3(1,0,1));
                float n011 = dot(Gradient(pi + float3(0,1,1)), pf - float3(0,1,1));
                float n111 = dot(Gradient(pi + float3(1,1,1)), pf - float3(1,1,1));

                // Trilinear interpolation
                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);

                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);

                return lerp(nxy0, nxy1, f.z) * 0.5 + 0.5;
            }

            float HenyeyGreenStein(float cosAngle, float g)
            {
                float g2 = g * g;
                float denom = 1.0 + g2 - 2.0 * g * cosAngle;
                return (1.0 / (4.0 * PI)) * (1.0 - g2) / pow(max(denom, 0.0001), 1.5);
            }

            float GetDesnity(float3 pos)
            {
                float3 noisePos = pos * _FogScale + (float3(_Time.y + Hash31(pos.xxx), _Time.y + Hash31(pos.yyy), _Time.y + Hash31(pos.zzz)) * _FogSpeed);
                float noise = Perlin3D(noisePos) * 0.5 + 0.5;
                float density = saturate(noise - 0.5) * _BaseDensity;
                return density;
            }

            float4 Frag(Varyings IN) : SV_Target {
                float2 uv = IN.texcoord;
                float rawDepth = SampleSceneDepth(uv);

                #if UNITY_REVERSED_Z
                    if (rawDepth < 0.00001) rawDepth = 0;
                #endif

                float3 worldPos = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 rayStart = _WorldSpaceCameraPos;
                float3 viewDir = worldPos - rayStart;
                float viewLength = length(viewDir);
                float3 rayDir = normalize(viewDir);

                float sceneDist = distance(rayStart, worldPos);
                float maxDist = min(sceneDist, 100.0);

                float jitter = InterleavedGradientNoise(uv * _ScreenParams.xy, (int)(_Time.y / max(HALF_EPS, unity_DeltaTime.x))) * _NoiseOffset;

                float3 transmittance = 1.0;
                float3 scattering = 0.0;

                uint pixelLightCount = GetAdditionalLightsCount();
                float stepSize = maxDist / (float)_StepCount;

                float dstTravelled = jitter * stepSize;

                for (int i = 0; i < MAX_STEP_COUNT; i++)
                {
                    if (i >= _StepCount) break;

                    float3 currentPos = rayStart + rayDir * dstTravelled;

                    float density = GetDesnity(currentPos); 

                    InputData inputData = (InputData) 0;
                    inputData.normalizedScreenSpaceUV = uv;
                    inputData.positionWS = currentPos;

                    half4 shadowCoord = CalculateShadowMask(inputData);
                    Light mainLight = GetMainLight(TransformWorldToShadowCoord(currentPos), currentPos, shadowCoord);
    
                    float3 mainLightStep = 
                        mainLight.color * 
                        mainLight.distanceAttenuation * 
                        mainLight.shadowAttenuation * 
                        HenyeyGreenStein(dot(rayDir, -mainLight.direction), _MainLightScattering) *
                        _MainLightIntensity;

                    if (mainLight.shadowAttenuation < 0.01) mainLightStep = 0.0;

                    float3 stepLight = 0.0;

                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, shadowCoord);

                        float3 lightContribution =
                            light.color *
                            light.distanceAttenuation *
                            light.shadowAttenuation *
                            HenyeyGreenStein(dot(rayDir, -light.direction), _AdditionalLightScattering) *
                            _AdditionalLightIntensity;

                        stepLight += lightContribution;
                    LIGHT_LOOP_END

                    float3 extinction = exp(-density * stepSize);
                    scattering += transmittance * stepLight * density * stepSize + transmittance *  mainLightStep * density;
                    transmittance *= extinction;


                    dstTravelled += stepSize;
                }

                //scattering = clamp(scattering, 0, 0.5);

                return float4(floor(scattering * 128.0) / 128.0, 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "Composite"
            ZTest Always ZWrite Off Blend Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            
            sampler2D _FogBuffer;
            float4 _FogBuffer_TexelSize;
            sampler2D _SceneTexture;

            float4 Frag(Varyings IN) : SV_Target {
                float2 uv = IN.texcoord;

                float4 fogSample = tex2D(_FogBuffer, uv);
                float3 fogColor = fogSample.rgb;

                float3 sceneColor = tex2D(_SceneTexture, uv).rgb;

                return float4(saturate(sceneColor + fogColor), 1.0);
            }
            ENDHLSL
        }
    }
}