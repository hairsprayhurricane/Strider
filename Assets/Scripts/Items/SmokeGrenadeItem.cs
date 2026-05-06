using UnityEngine;

[CreateAssetMenu(fileName = "SmokeGrenadeItem", menuName = "Items/SmokeGrenade")]
public class SmokeGrenadeItem : PlayerItem
{
    [Header("Smoke")]
    public int   cloudCount  = 8;
    public float smokeRadius = 10f;
    public float smokeSize   = 3f;
    public Color smokeColor  = new Color(0.55f, 0.55f, 0.55f, 1f);

    [Header("Duration")]
    public float duration = 10f;

    public override void Action() =>
        SpawnEffect(PlayerController.Instance.transform.position);

    public override void ActionAtPoint(Vector3 point) => SpawnEffect(point);

    private void SpawnEffect(Vector3 pos)
    {
        var go = new GameObject("SmokeGrenadeEffect");
        go.AddComponent<SmokeGrenadeEffectRunner>().Begin(
            smokeRadius, duration, smokeSize, smokeColor, cloudCount, pos);
    }
}
