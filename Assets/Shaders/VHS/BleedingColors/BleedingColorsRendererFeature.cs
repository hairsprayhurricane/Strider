using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/BleedingColors")]
public sealed class BleedingColors : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the bleeding colors.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(3f, 0f, 15f);

    [Tooltip("Degree of bleeding colors (shift).")]
    public ClampedFloatParameter shift = new ClampedFloatParameter(0.2f, -10f, 10f);

    public bool IsActive() => intensity.overrideState && intensity.value > 0f;
    public bool IsTileCompatible() => false;
}

public class BleedingColorsRendererFeature : ScriptableRendererFeature
{
    class BleedingColorsPass : ScriptableRenderPass
    {
        Material m_Material;

        public BleedingColorsPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float intensity;
            public float shift;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<BleedingColors>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_BleedingColorsDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("BleedingColors", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.intensity = effect.intensity.value;
                passData.shift = effect.shift.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_Intensity", data.intensity);
                    data.material.SetFloat("_ValueX", data.shift);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    BleedingColorsPass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/BleedingColorsVHS");
        if (shader == null) { Debug.LogError("Shader 'Hidden/BleedingColorsVHS' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new BleedingColorsPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}