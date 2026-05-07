Shader "Custom/Blit"
{
    Properties
    {
        [NoScaleOffset] _BlitTexture ("Blit Texture", 2D) = "none" {}
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

            Texture2D _BlitTexture;
            sampler sampler_BlitTexture;

            FragmentOutput FragmentPass(VertexOutput input)
            {
                FragmentOutput output;

                min16float4 sourceSample = _BlitTexture.Sample(sampler_BlitTexture, input.UV);
                //min16float4 blitSample   = _SourceTexture.Sample(sampler_BlitTexture, input.UV);

                //float invSrcAlpha = 1.0f - sourceSample.a;
                //float alpha       = sourceSample.a + (blitSample.a * invSrcAlpha);

                //min16float3 blendSource = sourceSample.rgb * sourceSample.a;
                //min16float3 blendBlit   = blitSample.rgb   * blitSample.a * invSrcAlpha;

                //float recipAlpha    = 1.0f / alpha;
                //min16float3 lerpRGB = (blendSource + blendBlit) * recipAlpha;
                output.colorOut     = sourceSample;

                return output;
            }

        ENDHLSL
        }
    }
}
