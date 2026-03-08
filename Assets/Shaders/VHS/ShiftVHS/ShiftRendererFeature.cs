using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

public class ShiftRendererFeature : ScriptableRendererFeature
{
    class ShiftPass : ScriptableRenderPass
    {
        Material m_Material;

        public ShiftPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float valueX;
            public float valueY;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<Shift>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_ShiftDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("Shift", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.valueX = effect.valueX.value;
                passData.valueY = effect.valueY.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_ValueX", data.valueX);
                    data.material.SetFloat("_ValueY", data.valueY);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    ShiftPass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/ShiftVHS");
        if (shader == null) { Debug.LogError("Shader 'Hidden/ShiftVHS' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new ShiftPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}