#ifndef LIGHTING_METHODS
#define LIGHTING_METHODS

// Shared Parameters
struct SharedLightingData
{
    min16float3 lightRGB;
    
    min16float3 N, L, V;
    
    float NDotL, NDotV;
    
    float roughness2;
};

struct BaseData
{
    float3 positionWS;
    float3 normalWS;
    
    float3 cameraPos;
    
    min16float3  lightDir;
    min16float3  lightRGB;
    float  distanceAttenuation, shadowAttenuation;
    
    float roughness;
};

SharedLightingData InitSharedData(in BaseData data)
{
    SharedLightingData output;
    output.lightRGB = data.lightRGB;
    
    output.N = data.normalWS;
    output.L = data.lightDir;
    output.V = normalize(data.cameraPos - data.positionWS);
    
    output.NDotL = saturate(dot(output.N, output.L));
    output.NDotV = saturate(dot(output.N, output.V));
    
    output.roughness2 = data.roughness * data.roughness;
    return output;
}

// Qualitative Oren-Nayar
float3 OrenNayar(in SharedLightingData data)
{        
    float thetaLN = acos(data.NDotL);
    float thetaNV = acos(data.NDotV);

    float alpha = max(thetaNV,  thetaLN);
    float beta  = min(thetaNV,  thetaLN);
    float gamma = cos(thetaNV - thetaLN);
    
    float roughness2 = data.roughness2;
    float A = 1.0f - 0.5f * (roughness2 / (roughness2 + 0.57f));
    float B = 0.45f * (roughness2 / (roughness2 + 0.09f));
    float C = sin(alpha) * tan(beta);
    
    float diffuse = data.NDotL * (A + (B * max(0.0f, gamma) * C));
    return data.lightRGB * diffuse;
}

static const float Pi = 3.1415926538;

float3 CookTorrance(in SharedLightingData data, float ior)
{
    min16float3 R = 2.0f * data.NDotL * data.N - data.L;
    min16float3 H = normalize(data.L + data.V);
    
    float NDotH = saturate(dot(data.N, H));
    float VDotH = saturate(dot(data.V, H));
    float HDotN = saturate(dot(H, data.N));
    
    float NDotH4 = NDotH * NDotH * NDotH * NDotH;
    
    float a = acos(NDotH);
    float m = clamp(data.roughness2, 0.01f, 1.0f);
    
    float exponent = exp(-tan(a) * tan(a) / (m * m));
    
    float D = clamp(exponent / (Pi * m * m * NDotH4), 1.0e-4f, 1.0e50f);
    
    float iorMinusOne = ior - 1.0f;
    float iorPlusOne  = ior + 1.0f;
    
    float F0     = (iorMinusOne * iorMinusOne) / (iorPlusOne * iorPlusOne);
    float denomF = 1.0f - clamp(VDotH, 0.0f, 1.0f);
    denomF  = denomF * denomF * denomF * denomF * denomF;
    float F = F0 + (1 - F0) * denomF;
    
    float G1 = 2.0f * HDotN * data.NDotV / VDotH;
    float G2 = 2.0f * HDotN * data.NDotL / VDotH;
    float G  = min(1.0f, min(G1, G2));
    
    float specular = (D * G * F) / (4.0f * data.NDotL) * data.NDotV;
    return data.lightRGB * specular;
}

#endif