#ifndef DEPTH_NORMALS_BASE
#define DEPTH_NORMALS_BASE

struct VertexInput
{
    float4 position : POSITION;
    half3 normal    : NORMAL;
    uint instanceID : INSTANCEID_SEMANTIC;
};

struct VertexOutput
{
    float4 position : SV_POSITION;
    half3  normal   : TEXCOORD0;
};

struct FragmentOutput
{
    float4 normalDepth : SV_TARGET;
};

VertexOutput VertexPass(VertexInput input)
{
    VertexOutput output;

    UNITY_SETUP_INSTANCE_ID(input);
    const VertexPositionInputs positionInputs = GetVertexPositionInputs(input.position.xyz);
    output.position = positionInputs.positionCS;
    output.normal   = TransformObjectToWorldNormal(input.normal);
    return output;
}

FragmentOutput FragmentPass(VertexOutput input)
{
    FragmentOutput output;
    output.normalDepth = float4(normalize(input.normal), input.position.w);
    return output;
}

#endif