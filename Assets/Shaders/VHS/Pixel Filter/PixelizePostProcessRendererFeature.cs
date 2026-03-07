using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/PixelizePostProcess")]
public sealed class PixelizePostProcess : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the size of the pixels (higher value means larger pixels).")]
    public ClampedFloatParameter pixelSize = new ClampedFloatParameter(10f, 1f, 100f);

    [Tooltip("Controls the intensity of the effect (0 = no effect, 1 = full effect).")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    public bool IsActive() => intensity.value > 0f;
    public bool IsTileCompatible() => false;
}

public class PixelizePostProcessRendererFeature : ScriptableRendererFeature
{
    class PixelizePass : ScriptableRenderPass
    {
        Material m_Material;

        public PixelizePass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float pixelSize;
            public float intensity;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<PixelizePostProcess>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_PixelizeDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("PixelizePostProcess", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.pixelSize = effect.pixelSize.value;
                passData.intensity = effect.intensity.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_PixelSize", data.pixelSize);
                    data.material.SetFloat("_Intensity", data.intensity);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    PixelizePass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/PixelizePostProcess");
        if (shader == null) { Debug.LogError("Shader 'Hidden/PixelizePostProcess' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new PixelizePass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}