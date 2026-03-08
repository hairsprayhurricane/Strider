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