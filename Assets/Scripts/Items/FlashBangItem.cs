using UnityEngine;

[CreateAssetMenu(fileName = "FlashbangItem", menuName = "Items/Flashbang")]
public class FlashBangItem : PlayerItem
{
    [Header("Lights")]
    public int lightCount = 6;
    public float lightRadius = 3f;
    public float maxLightIntensity = 15f;
    public float lightFadeDuration = 0.4f;

    [Header("Gamma Effect")]
    public float maxGamma = 1f;
    public float gammaDuration = 1.5f;

    [Header("Smoke")]
    public float smokeSize  = 2.5f;
    public Color smokeColor = new(0.82f, 0.82f, 0.82f, 1f);

    [Header("Timing")]
    public float delay = 0.5f;

    public override void Action()
    {
        var go = new GameObject("FlashBangEffect");
        var runner = go.AddComponent<FlashBangEffectRunner>();
        runner.Begin(lightCount, lightRadius, maxLightIntensity, lightFadeDuration,
            maxGamma, gammaDuration, delay, smokeSize, smokeColor);
    }
}
