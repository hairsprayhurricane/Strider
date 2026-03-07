using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/TintVHS")]
public sealed class Tint : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Y Shift.")]
    public FloatParameter y = new FloatParameter(1f);

    [Tooltip("U Shift.")]
    public FloatParameter u = new FloatParameter(1f);

    [Tooltip("V Shift.")]
    public FloatParameter v = new FloatParameter(1f);

    [Tooltip("Swap U and V channels.")]
    public BoolParameter swapUV = new BoolParameter(false);

    public bool IsActive() => y.value != 1f || u.value != 1f || v.value != 1f || swapUV.value;
    public bool IsTileCompatible() => false;
}

public class TintRendererFeature : ScriptableRendererFeature
{
    class TintPass : ScriptableRenderPass
    {
        Material m_Material;

        public TintPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float y, u, v;
            public bool swapUV;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<Tint>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_TintDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("Tint", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.y = effect.y.value;
                passData.u = effect.u.value;
                passData.v = effect.v.value;
                passData.swapUV = effect.swapUV.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_ValueX", data.y);
                    data.material.SetFloat("_ValueY", data.u);
                    data.material.SetFloat("_ValueZ", data.v);
                    data.material.SetFloat("_Switch", data.swapUV ? 1f : 0f);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    TintPass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/TintPostProcessVHS");
        if (shader == null) { Debug.LogError("Shader 'Hidden/TintPostProcessVHS' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new TintPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}