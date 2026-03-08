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