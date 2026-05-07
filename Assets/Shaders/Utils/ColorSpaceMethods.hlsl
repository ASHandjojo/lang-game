#ifndef COLOR_SPACE_METHODS
#define COLOR_SPACE_METHODS

float3 HueToRGB(in float H)
{
    float R = abs(H * 6.0f - 3.0f) - 1.0f;
    float G = 2.0f - abs(H * 6.0f - 2.0f);
    float B = 2.0f - abs(H * 6.0f - 4.0f);
    return saturate(float3(R, G, B));
}
              
float3 HSVToRGB(in float3 HSV)
{
    float3 RGB = HueToRGB(HSV.x);
    return ((RGB - 1) * HSV.y + 1) * HSV.z;
}

static const float Epsilon = 1.0e-10f;

float3 RGBToHCV(in float3 RGB)
{
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0f, 2.0f / 3.0f) : float4(RGB.gb, 0.0f, -1.0f / 3.0f);
    float4 Q = (RGB.r < P.x)   ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);

    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6.0f * C + Epsilon) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBToHSV(in float3 RGB)
{
    float3 HCV = RGBToHCV(RGB);
    float S    = HCV.y / (HCV.z + Epsilon);
    return float3(HCV.x, S, HCV.z);
}

float3 ApplyHSVToRGB(in float3 colorIn, float hue, float saturation, float value)
{
    float3 rgbToHSV = RGBToHSV(colorIn); // Converts to HSV

    // Is have the same function as Blender Hue (0.0 - 1.0, 0.5 == original color)
    rgbToHSV.x = rgbToHSV.x * (hue + 0.5f);
    rgbToHSV.y = rgbToHSV.y * saturation;
    rgbToHSV.z = rgbToHSV.z * value;
    
    return HSVToRGB(rgbToHSV); // Converts back to RGB
}

#endif