using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;


[Serializable, VolumeComponentMenu("Post-processing/Custom/ScanlinesVHS")]
public sealed class Scanlines : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the scanlines.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    [Tooltip("Scanline color.")]
    public ColorParameter color = new ColorParameter(new Color(0f, 0f, 0f, 1f));

    [Tooltip("Lines size.")]
    public ClampedFloatParameter valueX = new ClampedFloatParameter(1f, 1f, 10f);

    public bool IsActive() => intensity.value > 0f;
    public bool IsTileCompatible() => false;
}
