using UnityEngine;

[CreateAssetMenu(fileName = "FlashbangItem", menuName = "Items/Flashbang")]
public class FlashBangItem : PlayerItem
{
    [Header("Lights")]
    public int lightCount = 6;
    public float lightRadius = 10f;
    public float maxLightIntensity = 15f;
    public float lightFadeDuration = 0.4f;

    [Header("Gamma Effect")]
    public float maxGamma = 1f;
    public float gammaDuration = 1.5f;

    [Header("Smoke")]
    public float smokeSize  = 5f;
    public Color smokeColor = new(0.82f, 0.82f, 0.82f, 1f);

    [Header("Timing")]
    public float delay = 0.5f;

    [Header("Enemy Stun")]
    public float enemyStunRadius = 10f;
    public float enemyStunDuration = 5f;

    public override void Action() =>
        SpawnEffect(PlayerController.Instance.transform.position);

    public override void ActionAtPoint(Vector3 point) => SpawnEffect(point);

    private void SpawnEffect(Vector3 pos)
    {
        var go = new GameObject("FlashBangEffect");
        go.AddComponent<FlashBangEffectRunner>().Begin(
            lightCount, lightRadius, maxLightIntensity, lightFadeDuration,
            maxGamma, gammaDuration, delay, smokeSize, smokeColor,
            enemyStunRadius, enemyStunDuration, pos);
    }
}
