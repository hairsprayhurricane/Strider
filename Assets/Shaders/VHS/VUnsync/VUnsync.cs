using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/VUnsyncVHS")]
public sealed class VUnsync : VolumeComponent, IPostProcessComponent
{
    [Tooltip("Height shift value.")]
    public ClampedFloatParameter valueX = new ClampedFloatParameter(0.5f, -1f, 1f);

    public bool IsActive() => Mathf.Abs(valueX.value) > 0f;
    public bool IsTileCompatible() => false;
}
