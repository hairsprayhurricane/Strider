using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Thunderbolt : PlayerGun
{
    private const byte lightningAmount = 3;
    public float maxLength = 100f;
    public float segmentLength = 2f;
    public int maxSegments = 50;
    public float displacement = 1.5f;
    public float fadeTime = 0.1f;
    public Material lightningMainMat;

    [Header("Branches")]
    public int maxBranchDepth = 2;
    public float branchChance = 0.3f;
    public float branchLengthMultiplier = 0.45f;
    public float branchDisplacementMultiplier = 1.4f;

    [Header("Light")]
    public float lightRange = 8f;
    public float lightIntensity = 3f;
    public Color lightColor = new Color(0.6f, 0.75f, 1f);

    public override string GetDescription()
    {
        return "-";
    }

    public override void Fire()
    {
        Vector3 spawnPos = playerPos.position;
        Vector3 baseDir = playerPos.forward;
        baseDir.y = 0f;
        baseDir.Normalize();

        float maxAngle = bulletSpread;
        float azimuth = ClassicRandom.Range(0f, 360f);
        float angle = ClassicRandom.Range(0f, maxAngle);

        Vector3 ortho = Vector3.Cross(baseDir, Vector3.up);
        if (ortho.sqrMagnitude < 1e-6f)
            ortho = Vector3.Cross(baseDir, Vector3.right);
        ortho.Normalize();
        Vector3 ortho2 = Vector3.Cross(baseDir, ortho).normalized;

        Vector3 spreadAxis =
            ortho * Mathf.Cos(azimuth * Mathf.Deg2Rad) +
            ortho2 * Mathf.Sin(azimuth * Mathf.Deg2Rad);

        Vector3 offsetDir =
            (Quaternion.AngleAxis(angle, spreadAxis) * baseDir).normalized;

        SpawnLightning(spawnPos, offsetDir);
        AfterFire();
    }

    void SpawnLightning(Vector3 startPos, Vector3 direction)
    {
        Ray ray = new Ray(startPos, direction);
        bool hit = Physics.Raycast(ray, out RaycastHit hitInfo, maxLength);
        Vector3 endPos = hit ? hitInfo.point : startPos + direction * maxLength;

        for (int i = 0; i < lightningAmount; i++)
        {
            LineRenderer lr = CreateLightningRenderer();
            GenerateLightningBolt(lr, startPos, endPos, displacement, 0);
            StartCoroutine(FadeLightning(lr));
        }

        if (hit) ApplyDamage(hitInfo);

        SpawnLightningLight(Vector3.Lerp(startPos, endPos, 0.5f));
    }

    void GenerateLightningBolt(LineRenderer lr, Vector3 start, Vector3 end,
                                float disp, int depth)
    {
        Vector3 dir = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        int segments = Mathf.Max(2, Mathf.Min(maxSegments,
                                 Mathf.FloorToInt(distance / segmentLength)));

        var points = new List<Vector3>(segments + 1);
        points.Add(start);

        Vector3 perp1 = Vector3.Cross(dir, Vector3.up).normalized;
        if (perp1.sqrMagnitude < 1e-6f)
            perp1 = Vector3.Cross(dir, Vector3.right).normalized;
        Vector3 perp2 = Vector3.Cross(dir, perp1).normalized;

        Vector3 current = start;
        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 target = Vector3.Lerp(start, end, t);

            float n1 = (ClassicRandom.value - 0.5f) * disp;
            float n2 = (ClassicRandom.value - 0.5f) * disp;
            current = target + perp1 * n1 + perp2 * n2;
            points.Add(current);

            if (depth < maxBranchDepth && ClassicRandom.value < branchChance)
                SpawnBranch(current, dir, distance, disp, depth);
        }

        points.Add(end);
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
    }

    void SpawnBranch(Vector3 origin, Vector3 parentDir, float parentLength, float disp, int depth)
    {
        float yaw   = ClassicRandom.Range(-55f, 55f);
        float pitch = ClassicRandom.Range(-35f, 35f);
        Vector3 branchDir = Quaternion.Euler(pitch, yaw, 0f) * parentDir;
        branchDir.Normalize();

        float branchLength = parentLength * branchLengthMultiplier * ClassicRandom.Range(0.5f, 1f);
        Vector3 branchEnd = origin + branchDir * branchLength;

        LineRenderer lr = CreateLightningRenderer(isBranch: true);
        GenerateLightningBolt(lr, origin, branchEnd, disp * branchDisplacementMultiplier, depth + 1);
        SpawnLightningLight(origin);
        StartCoroutine(FadeLightning(lr));
    }

    // ─── fabs ────────────────────────────────────────────────────────────

    LineRenderer CreateLightningRenderer(bool isBranch = false)
    {
        GameObject go = new GameObject(isBranch ? "LightningBranch" : "LightningBolt");
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();

        lr.material = lightningMainMat;
        lr.material.color = new Color(0.55f, 0.75f, 1f);
        lr.widthMultiplier = isBranch ? 0.025f : 0.05f;
        lr.numCapVertices = 3;
        lr.useWorldSpace = true;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        return lr;
    }

    void SpawnLightningLight(Vector3 position)
    {
        GameObject go = new GameObject("LightningLight");
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.intensity = lightIntensity;
        light.range = lightRange;
        light.shadows = LightShadows.None;

        StartCoroutine(FadeLight(light, fadeTime * 2f));
    }

    // ─── cors ───────────────────────────────────────────────────────────

    IEnumerator FadeLightning(LineRenderer lr)
    {
        float timer = 0f;
        Color startColor = lr.material.color;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeTime);
            lr.material.color = c;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    IEnumerator FadeLight(Light light, float duration)
    {
        float startIntensity = light.intensity;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            light.intensity = Mathf.Lerp(startIntensity, 0f, timer / duration);
            yield return null;
        }
        Destroy(light.gameObject);
    }

    // ─── damage ───────────────────────────────────────────────────────────────

    void ApplyDamage(RaycastHit hit)
    {
        switch (hit.collider.tag)
        {
            case "Enemy":
                hit.collider.GetComponent<Enemy>()?.TakeDamage(damage);
                break;
            case "Player":
                hit.collider.GetComponent<PlayerHealth>()?.DamagePlayer(damage);
                break;
            case "ExplosiveObject":
                var barrel = hit.collider.GetComponent<RedBarrel>();
                if (barrel != null)
                {
                    barrel.health -= damage;
                    if (barrel.health <= 0) barrel.Boom();
                }
                break;
        }
    }
}