Shader "Custom/BasePBR"
{
    Properties
    {
        [NoScaleOffset]          _BaseColor ("BaseColor", 2D) = "none" {}
        [NoScaleOffset] [Normal] _Normal    ("Normal",    2D) = "none" {}
        [NoScaleOffset]          _Metallic  ("Metallic",  2D) = "none" {}
        [NoScaleOffset]          _Roughness ("Roughness", 2D) = "none" {}

        _Hue        ("Hue",        float) = 0.5
        _Saturation ("Saturation", float) = 1.0
        _Value      ("Value",      float) = 1.0 

        _IOR ("Index of Refraction", float) = 1.0
    }
    SubShader
    {
        Pass
        {
            ZWrite On
            ZTest LEqual

            Tags
            {
                "RenderType" = "Opaque"
                "LightMode"  = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 5.0
            #pragma vertex VertexPass
            #pragma fragment FragmentPass
            #pragma multi_compile_instancing
            #pragma enable_cbuffer

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Assets/Shaders/Utils/ColorSpaceMethods.hlsl"
            #include "Assets/Shaders/Utils/Interpolation.hlsl"
            #include "Assets/Shaders/Utils/Lighting.hlsl"
            #include "Assets/Shaders/Utils/PBR.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float _IOR;
            float _Hue, _Saturation, _Value;
            CBUFFER_END

            struct FragmentOutput
            {
                min16float3 RGB : COLOR0;
            };

            Texture2D _BaseColor, _Normal, _Roughness, _Metallic;
            sampler sampler_trilinear_aniso16_repeat_BaseColor, sampler_trilinear_aniso16_repeat_Normal, sampler_trilinear_aniso16_repeat_Roughness, sampler_trilinear_aniso16_repeat_Metallic;

            FragmentOutput FragmentPass(VertexOutput input)
            {
                FragmentOutput output;
                UNITY_SETUP_INSTANCE_ID(input);

                float2 scaledUV = input.UV * 3.0f;

                DiffuseData diffuseData;
                diffuseData.baseColor = _BaseColor.Sample(sampler_trilinear_aniso16_repeat_BaseColor, scaledUV).rgb;
                diffuseData.roughness = _Roughness.Sample(sampler_trilinear_aniso16_repeat_Roughness, scaledUV).x;
                diffuseData.normal    = _Normal.Sample(sampler_trilinear_aniso16_repeat_Normal, scaledUV).xyz; // In tangent space

                float3 hsv = RGBToHSV(diffuseData.baseColor);
                hsv.r      = frac(hsv.r + (_Hue - 0.5f));
                hsv.g      = hsv.g * _Saturation;
                hsv.b      = hsv.b * _Value;
                diffuseData.baseColor = HSVToRGB(hsv);
                
                SpecularData specularData;
                specularData.metallic = _Metallic.Sample(sampler_trilinear_aniso16_repeat_Metallic, scaledUV).x;
                specularData.ior      = _IOR;

                AmbientOcclusionFactor aoFac = GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(input.position));
                Light light             = GetMainLight();
                BaseData baseData       = CreateBaseData(input, _WorldSpaceCameraPos, light, diffuseData);
                SharedLightingData data = InitSharedData(baseData);

                output.RGB = CalculateDirect(baseData, data, diffuseData, specularData);

                output.RGB += CalculateIndirect(baseData, diffuseData, aoFac);
                output.RGB *= max(MainLightRealtimeShadow(input.shadowCoords), 0.5f);
                //output.RGB = light.distanceAttenuation;
                return output;
            }
            ENDHLSL
        }

        Pass
        {
            ZWrite On
            ZTest LEqual

            Name "DepthOnlyPass"
            Tags
            {
                "LightMode"  = "DepthOnlyPass"
            }

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Assets/Shaders/Utils/DepthOnlyBase.hlsl"

            #pragma target 5.0
            #pragma vertex VertexPass
            #pragma fragment FragmentPass
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma enable_cbuffer

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END
            ENDHLSL
        }

        Pass
        {
            ZWrite On
            ZTest LEqual

            Name "DepthNormalsPass"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Assets/Shaders/Utils/DepthNormalsBase.hlsl"

            #pragma target 5.0
            #pragma vertex VertexPass
            #pragma fragment FragmentPass
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma enable_cbuffer

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            ENDHLSL
        }
    }
}
