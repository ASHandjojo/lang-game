#ifndef DEPTH_ONLY_METHODS
#define DEPTH_ONLY_METHODS

struct VertexInput
{
    float4 position : POSITION;
    uint instanceID : INSTANCEID_SEMANTIC;
};

struct VertexOutput
{
    float4 position : SV_POSITION;
};

struct FragmentOutput
{
    float depth : SV_TARGET;
};

VertexOutput VertexPass(VertexInput input)
{
    VertexOutput output;

    UNITY_SETUP_INSTANCE_ID(input);
    
    const VertexPositionInputs positionInputs = GetVertexPositionInputs(input.position.xyz);
    output.position = positionInputs.positionCS;
    return output;
}

FragmentOutput FragmentPass(VertexOutput input)
{
    FragmentOutput output;
    output.depth = input.position.w;
    return output;
}

#endif