using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

public class DistortionRendererFeature : ScriptableRendererFeature
{
    class DistortionPass : ScriptableRenderPass
    {
        Material m_Material;

        public DistortionPass(Material material)
        {
            m_Material = material;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        }

        class PassData
        {
            public TextureHandle src;
            public Material material;
            public float intensity;
            public float valueX;
            public Texture displacementTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var stack = VolumeManager.instance.stack;
            var effect = stack.GetComponent<Distortion>();
            if (effect == null || !effect.IsActive()) return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var src = resourceData.activeColorTexture;

            var desc = renderGraph.GetTextureDesc(src);
            desc.name = "_DistortionDst";
            desc.clearBuffer = false;
            var dst = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddUnsafePass<PassData>("Distortion", out var passData))
            {
                passData.src = src;
                passData.material = m_Material;
                passData.intensity = effect.intensity.value;
                passData.valueX = effect.valueX.value;
                passData.displacementTexture = effect.displacementTexture.value;

                builder.UseTexture(src, AccessFlags.Read);
                builder.UseTexture(dst, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
                {
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.material.SetFloat("_Intensity", data.intensity);
                    data.material.SetFloat("_ValueX", data.valueX);
                    data.material.SetTexture("_Texture", data.displacementTexture);
                    Blitter.BlitCameraTexture(cmd, data.src, dst, data.material, 0);
                    Blitter.BlitCameraTexture(cmd, dst, data.src);
                });
            }
        }
    }

    Material m_Material;
    DistortionPass m_Pass;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/DistortionVHS");
        if (shader == null) { Debug.LogError("Shader 'Hidden/DistortionVHS' not found."); return; }
        m_Material = CoreUtils.CreateEngineMaterial(shader);
        m_Pass = new DistortionPass(m_Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (m_Pass != null) renderer.EnqueuePass(m_Pass);
    }

    protected override void Dispose(bool disposing) => CoreUtils.Destroy(m_Material);
}