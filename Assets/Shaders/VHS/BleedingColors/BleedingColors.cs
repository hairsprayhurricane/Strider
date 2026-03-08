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