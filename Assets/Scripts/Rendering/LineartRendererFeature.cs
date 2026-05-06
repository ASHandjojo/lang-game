using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public sealed class LineartRendererFeature : ScriptableRendererFeature
{
    [SerializeField, HideInInspector] private Material lineartMaterial;
    [SerializeField, HideInInspector] private Material blitMaterial;
    private CustomPostRenderPass fullScreenPass;

    private static readonly int FilterTextureID = Shader.PropertyToID("_FilterTexture");
    private static readonly int DepthTextureID  = Shader.PropertyToID("_CameraDepthTexture");

    private static readonly int ThresholdValID  = Shader.PropertyToID("_Threshold");

    private static readonly int SourceTextureID = Shader.PropertyToID("_BlitTexture");

    public override void Create()
    {
#if UNITY_EDITOR
        lineartMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Post Processing/Lineart.mat");
        blitMaterial    = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Shaders/Post Processing/Blit.mat");
#endif
        if (lineartMaterial != null && blitMaterial != null)
        {
            fullScreenPass = new CustomPostRenderPass(name, lineartMaterial, blitMaterial);
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (lineartMaterial == null || fullScreenPass == null)
        {
            return;
        }
        // This check makes sure to not render the effect to reflection probes or preview cameras as post-processing is typically not desired there
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
        {
            return;
        }

        LineartVolumeComponent volume = VolumeManager.instance.stack?.GetComponent<LineartVolumeComponent>();
        if (volume == null || !volume.IsActive())
        {
            return;
        }

        fullScreenPass.renderPassEvent = RenderPassEvent.AfterRendering;
        fullScreenPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(fullScreenPass);
    }

    protected override void Dispose(bool disposing) {}

    private sealed class CustomPostRenderPass : ScriptableRenderPass
    {
        private Material lineartMaterial, blitMaterial;

        private static MaterialPropertyBlock sharedPropertyBlock = new();

        public CustomPostRenderPass(string passName, Material lineartMaterial, Material blitMaterial)
        {
            profilingSampler     = new ProfilingSampler(passName);
            this.lineartMaterial = lineartMaterial;
            this.blitMaterial    = blitMaterial;
        }

        private sealed class CopyPassData
        {
            public TextureHandle inputTexture;
        }

        private sealed class LineArtData
        {
            public Material lineartMaterial, blitMaterial;
            public TextureHandle edgeTexture, colorCameraTexture, depthTexture;

            public TextureHandle rttiTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData  resourcesData   = frameData.Get<UniversalResourceData>();
            UniversalCameraData    cameraData      = frameData.Get<UniversalCameraData>();
            LineartVolumeComponent volume          = VolumeManager.instance.stack.GetComponent<LineartVolumeComponent>();

            // Line Art
            using (var builder = renderGraph.AddRasterRenderPass<LineArtData>(passName, out var passData))
            {
                passData.lineartMaterial = lineartMaterial;
                passData.blitMaterial    = blitMaterial;

                var cameraColorDesc         = renderGraph.GetTextureDesc(resourcesData.cameraColor);
                cameraColorDesc.name        = "EdgeTexture";
                cameraColorDesc.clearBuffer = false;

                TextureHandle edgeTexture = renderGraph.CreateTexture(cameraColorDesc);
                passData.edgeTexture      = edgeTexture;

                passData.colorCameraTexture = resourcesData.cameraColor;
                passData.depthTexture       = resourcesData.cameraDepthTexture;

                builder.UseTexture(passData.colorCameraTexture, AccessFlags.Read);
                builder.SetRenderAttachment(edgeTexture, 0,     AccessFlags.Write);
                builder.SetRenderAttachmentDepth(resourcesData.cameraDepth, AccessFlags.Read);

                builder.SetRenderFunc(
                    (LineArtData passData, RasterGraphContext context) =>
                    {
                        passData.lineartMaterial.SetTexture(FilterTextureID, passData.colorCameraTexture);
                        passData.lineartMaterial.SetTexture(DepthTextureID,  passData.depthTexture);

                        passData.lineartMaterial.SetFloat(ThresholdValID, volume.threshold.value);

                        context.cmd.DrawProcedural(Matrix4x4.identity, passData.lineartMaterial, -1, MeshTopology.Triangles, 3, 1);
                    }
                );
                resourcesData.cameraColor = passData.edgeTexture;
            }
            // Write Back to Color Texty
            using (var builder = renderGraph.AddRasterRenderPass<LineArtData>(passName + "Blit", out var passData))
            {
                builder.SetRenderFunc(
                    (LineArtData passData, RasterGraphContext context) =>
                    {
                        builder.SetRenderAttachment(passData.colorCameraTexture, 0);
                        passData.blitMaterial.SetTexture(SourceTextureID, resourcesData.cameraColor);

                        context.cmd.DrawProcedural(Matrix4x4.identity, passData.blitMaterial, -1, MeshTopology.Triangles, 3, 1);
                    }
                );
            }
        }
    }
}