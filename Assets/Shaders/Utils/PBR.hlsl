#ifndef PBR_BASE_METHODS
#define PBR_BASE_METHODS

struct VertexInput
{
    float4 position : POSITION;
    float3 normal   : NORMAL;
    float2 UV       : TEXCOORD0;
    // [x, y, z] = Generated Coordinates, [w] = RTTI
    min16float4 packedCoords : COLOR0;
    
    uint instanceID : INSTANCEID_SEMANTIC;
};

struct VertexOutput
{
    float4 position   : SV_POSITION;
    float2 UV         : TEXCOORD0;

    float3 positionWS  : TEXCOORD1;
    float3 normalWS    : TEXCOORD2;
    float3 tangentWS   : TEXCOORD3;
    float3 bitangentWS : TEXCOORD4;
    
    min16float4 packedCoords : COLOR0;

    float4 shadowCoords : TEXCOORD5;

    uint instanceID   : CUSTOM_INSTANCE_ID;
};

VertexOutput VertexPass(VertexInput input)
{
    VertexOutput output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    const VertexPositionInputs positionInputs = GetVertexPositionInputs(input.position.xyz);
    output.position   = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;

    const VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normal);
    output.normalWS    = normalize(normalInputs.normalWS);
    output.tangentWS   = normalize(normalInputs.tangentWS);
    output.bitangentWS = normalize(normalInputs.bitangentWS);

    output.shadowCoords = GetShadowCoord(positionInputs);
    
    output.packedCoords = input.packedCoords;

    output.UV = input.UV;
    return output;
}

struct DiffuseData
{
    min16float3 baseColor;
    // Sampled from texture
    float3 normal;
    float  roughness;
};

struct SpecularData
{
    float metallic;
    float ior;
};

float3 TangentToWorldSpace(in VertexOutput input, in float3 normal)
{
    float3x3 tangentBasis = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
    normal = normalize(mul(tangentBasis, normal));
    return normal;
}

BaseData CreateBaseData(in VertexOutput input, in float3 cameraPos, in Light mainLight, in DiffuseData diffuseData)
{
    BaseData output;
    output.positionWS = input.positionWS;
    output.normalWS   = TangentToWorldSpace(input, diffuseData.normal);
    
    output.cameraPos = cameraPos;
    
    output.lightDir = mainLight.direction;
    output.lightRGB = mainLight.color;
    output.distanceAttenuation = mainLight.distanceAttenuation;
    output.shadowAttenuation   = mainLight.shadowAttenuation;
    
    output.roughness = diffuseData.roughness;
    
    return output;
}

struct DiffuseOutput
{
    min16float3 diffuse;
};

struct DiffuseSpecularOutput
{
    min16float3 diffuse, specular;
};

// Diffuse only
DiffuseOutput CalculateDiffuse(in SharedLightingData data, in DiffuseData diffuseData)
{    
    DiffuseOutput output;
    output.diffuse = OrenNayar(data);
    return output;
}

DiffuseSpecularOutput CalculateDiffuseSpecular(in SharedLightingData data, in DiffuseData diffuseData, in SpecularData specularData)
{    
    DiffuseSpecularOutput output;
    output.diffuse  = OrenNayar(data);
    output.specular = CookTorrance(data, specularData.ior);
    return output;
}

// Diffuse Only
float3 CalculateDirect(in BaseData baseData, in SharedLightingData data, in DiffuseData diffuseData)
{    
    float attenuation   = baseData.distanceAttenuation * baseData.shadowAttenuation;
    
    DiffuseOutput lighting = CalculateDiffuse(data, diffuseData);
    float3 diffuse = lighting.diffuse * attenuation;
    
    float3 output = diffuseData.baseColor * diffuse;
    return attenuation;
}

// Combined
min16float3 CalculateDirect(in BaseData baseData, in SharedLightingData data, in DiffuseData diffuseData, in SpecularData specularData)
{    
    float attenuation = baseData.distanceAttenuation * baseData.shadowAttenuation;
    
    DiffuseSpecularOutput lighting = CalculateDiffuseSpecular(data, diffuseData, specularData);
    min16float3 diffuse = lighting.diffuse * attenuation;
    min16float3 specular = lighting.specular * attenuation;

    min16float3 mixedActive = (lerp(diffuse, specular, specularData.metallic));

    min16float3 output = diffuseData.baseColor * mixedActive;
    return output;
}

min16float3 CalculateIndirect(in BaseData baseData, in DiffuseData diffuseData, in AmbientOcclusionFactor aoFac)
{
    min16float3 ambientSH    = SampleSHVertex(baseData.normalWS);
    min16float3 ambientColor = min16float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w) * aoFac.directAmbientOcclusion;
    return ambientColor * diffuseData.baseColor;
}

#endif