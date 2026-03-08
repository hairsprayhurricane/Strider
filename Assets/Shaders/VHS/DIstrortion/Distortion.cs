using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Distortion")]
public sealed class Distortion : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0.01f, 0f, 1f);

    [Tooltip("Noise value.")]
    public ClampedFloatParameter valueX = new ClampedFloatParameter(4.51f, 0f, 10f);

    [Tooltip("Displacement map texture.")]
    public TextureParameter displacementTexture = new TextureParameter(null);

    public bool IsActive() => intensity.value > 0f;
    public bool IsTileCompatible() => false;
}