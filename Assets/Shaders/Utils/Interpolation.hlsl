#ifndef INTERPOLATION_METHODS
#define INTERPOLATION_METHODS

static const int AlphaPower = 2;

// Does not saturate fac internally
float ParametricSmooth(float fac)
{
    float powFac      = fac;
    float oneMinusFac = 1.0f - fac;
    float powInvFac   = oneMinusFac;
    [unroll]
    for (int i = 0; i < AlphaPower - 1; i++)
    {
        powFac    *= fac;
        powInvFac *= oneMinusFac;
    }
    
    return powFac / (powFac + powInvFac);
}

float2 ParametricSmooth(float2 fac)
{
    float2 powFac      = fac;
    float2 oneMinusFac = 1.0f - fac;
    float2 powInvFac   = oneMinusFac;
    [unroll]
    for (int i = 0; i < AlphaPower - 1; i++)
    {
        powFac    *= fac;
        powInvFac *= oneMinusFac;
    }
    
    return powFac / (powFac + powInvFac);
}

float3 ParametricSmooth(in float3 fac)
{
    float3 powFac = fac;
    float3 oneMinusFac = 1.0f - fac;
    float3 powInvFac = oneMinusFac;
    [unroll]
    for (int i = 0; i < AlphaPower - 1; i++)
    {
        powFac *= fac;
        powInvFac *= oneMinusFac;
    }
    
    return powFac / (powFac + powInvFac);
}

float4 ParametricSmooth(in float4 fac)
{
    float4 powFac = fac;
    float4 oneMinusFac = 1.0f - fac;
    float4 powInvFac = oneMinusFac;
    [unroll]
    for (int i = 0; i < AlphaPower - 1; i++)
    {
        powFac *= fac;
        powInvFac *= oneMinusFac;
    }
    
    return powFac / (powFac + powInvFac);
}


float ParaSmoothStep(float min, float max, float fac)
{
    float extent = max - min;
    fac = saturate((fac - min) / extent); // 0 to 1 normalization
    return ParametricSmooth(fac);
}

float2 ParaSmoothStep(float2 min, float2 max, float2 fac)
{
    float2 extent = max - min;
    fac = saturate((fac - min) / extent); // 0 to 1 normalization
    return ParametricSmooth(fac);
}

float3 ParaSmoothStep(in float3 min, in float3 max, in float3 fac)
{
    float3 extent = max - min;
    fac = saturate((fac - min) / extent); // 0 to 1 normalization
    return ParametricSmooth(fac);
}


float4 ParaSmoothStep(in float4 min, in float4 max, in float4 fac)
{
    float4 extent = max - min;
    fac = saturate((fac - min) / extent); // 0 to 1 normalization
    return ParametricSmooth(fac);
}

#endif