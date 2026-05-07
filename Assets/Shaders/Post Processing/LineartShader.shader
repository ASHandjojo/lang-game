Shader "Custom/LineartShader"
{
    Properties
    {
        [HideInInspector] _Threshold ("Gradient Threshold", float) = 0.0075
    }
    SubShader
    {
        Pass
        {
            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex VertexPass
            #pragma fragment FragmentPass
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Assets/Shaders/Utils/Interpolation.hlsl"
            #include "Assets/Shaders/Utils/ColorSpaceMethods.hlsl"

            struct VertexInput
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 UV       : TEXCOORD0;
            };

            struct FragmentOutput
            {
                half4 colorOut : SV_TARGET0;
            };

            VertexOutput VertexPass(VertexInput input)
            {
                VertexOutput output;

                output.position = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.UV       = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            Texture2D _CameraDepthTexture, _FilterTexture;
            sampler sampler_CameraDepthTexture, sampler_FilterTexture;

            float _Threshold;

            FragmentOutput FragmentPass(VertexOutput input)
            {
                FragmentOutput output;

                float2 dimCast = _ScreenParams.zw - 1.0f;
                float dimMag   = length(dimCast);

                // Reversed Z order in Vulkan
                float depth      = 1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, input.UV).r;
                float4 cardinals = float4(
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV - float2(dimCast.x, 0.0f), 0.0f, 1.0f)).r,
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV + float2(dimCast.x, 0.0f), 0.0f, 1.0f)).r,
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV - float2(0.0f, dimCast.y), 0.0f, 1.0f)).r,
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV + float2(0.0f, dimCast.y), 0.0f, 1.0f)).r
                );

                float4 ordinals = float4(
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV + float2(-dimCast.x,  dimCast.y), 0.0f, 1.0f)).r,
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV + float2(dimCast.x,   dimCast.y), 0.0f, 1.0f)).r,
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV + float2(-dimCast.x, -dimCast.y), 0.0f, 1.0f)).r,
                    1.0f - _CameraDepthTexture.Sample(sampler_CameraDepthTexture, clamp(input.UV + float2(dimCast.x,  -dimCast.y), 0.0f, 1.0f)).r
                );

                float3x3 pixels = {
                    ordinals.x,  cardinals.w, ordinals.y,
                    cardinals.x, depth,       cardinals.y,
                    ordinals.z,  cardinals.z, ordinals.w,
                };

                float gradientX = dot(float3(3.0f, 10.0f, 3.0f), mul(pixels, float3(3.0f, 0.0f, -3.0f)));
                float gradientY = dot(float3(3.0f, 0.0f, -3.0f), mul(pixels, float3(3.0f, 10.0f, 3.0f)));
                float gradient  = length(float2(gradientX, gradientY));

                float angle = atan2(gradientY, gradientX) / gradient;
                float edge  = gradient;

                float4 originalColor = _FilterTexture.Sample(sampler_FilterTexture, input.UV);
                output.colorOut      = edge < _Threshold ? originalColor : float4(0.0f, 0.0f, 0.0f, 1.0f);

                return output;
            }

        ENDHLSL
        }
    }
}
