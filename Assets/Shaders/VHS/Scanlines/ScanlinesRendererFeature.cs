using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;


public class ScanlinesRendererFeature : ScriptableRendererFeature
{
    class ScanlinesPass : ScriptableRenderPass
    {
        Material m_Material;

        public ScanlinesPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float intensity;
            public Color color;
            public float valueX;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<Scanlines>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_ScanlinesDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("Scanlines", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.intensity = effect.intensity.value;
                passData.color = effect.color.value;
                passData.valueX = effect.valueX.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_Intensity", data.intensity);
                    data.material.SetColor("_Color", data.color);
                    data.material.SetFloat("_ValueX", data.valueX);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    ScanlinesPass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/ScanlinesVHS");
        if (shader == null) { Debug.LogError("Shader 'Hidden/ScanlinesVHS' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new ScanlinesPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}