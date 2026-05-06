using UnityEngine;
using UnityEngine.Rendering;

using UnityEngine.Rendering.Universal;

[VolumeComponentMenu("Post-processing Custom/Lineart")]
[VolumeRequiresRendererFeatures(typeof(LineartRendererFeature))]
[SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
public sealed class LineartVolumeComponent : VolumeComponent, IPostProcessComponent
{
    public LineartVolumeComponent()
    {
        displayName = "Lineart";
    }

    public FloatParameter threshold = new(0.0f, false);

    public bool IsActive() => active && threshold.value > 0.0f;
}
