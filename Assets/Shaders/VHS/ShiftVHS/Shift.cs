using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/ShiftVHS")]
public sealed class Shift : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Horizontal Shift.")]
    public ClampedFloatParameter valueX = new ClampedFloatParameter(0.1f, -1f, 1f);

    [Tooltip("Vertical Shift.")]
    public ClampedFloatParameter valueY = new ClampedFloatParameter(0f, 0f, 1f);

    public bool IsActive() => (valueX.overrideState && Mathf.Abs(valueX.value) > 0f) || 
                          (valueY.overrideState && Mathf.Abs(valueY.value) > 0f);
    public bool IsTileCompatible() => false;
}
