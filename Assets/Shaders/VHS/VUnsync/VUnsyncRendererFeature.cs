using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

public class VUnsyncRendererFeature : ScriptableRendererFeature
{
    class VUnsyncPass : ScriptableRenderPass
    {
        Material m_Material;

        public VUnsyncPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float valueX;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<VUnsync>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_VUnsyncDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("VUnsync", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.valueX = effect.valueX.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_ValueX", data.valueX);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    VUnsyncPass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/VUnsyncVHS");
        if (shader == null) { Debug.LogError("Shader 'Hidden/VUnsyncVHS' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new VUnsyncPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}