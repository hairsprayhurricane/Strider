using UnityEngine;

public class Thunderbolt : PlayerGun
{
    public LineRenderer lightningPrefab;
    private const byte lightningAmount = 3;
    public float maxLength = 100f;
    public float segmentLength = 2f;
    public int maxSegments = 50;
    public float displacement = 1.5f;
    public float fadeTime = 0.1f;

    public override void Fire()
    {
        audioSource.Stop();

        Vector3 spawnPos = playerPos.position;
        Vector3 baseDir = playerPos.forward;
        baseDir.y = 0f;
        baseDir.Normalize();

        float maxAngle = bulletSpread;
        float azimuth = ClassicRandom.Range(0f, 360f);
        float angle = ClassicRandom.Range(0f, maxAngle);

        Vector3 axis = baseDir;
        Vector3 ortho = Vector3.Cross(axis, Vector3.up);
        if (ortho.sqrMagnitude < 1e-6f)
            ortho = Vector3.Cross(axis, Vector3.right);

        ortho.Normalize();
        Vector3 ortho2 = Vector3.Cross(axis, ortho).normalized;

        Vector3 spreadAxis =
            ortho * Mathf.Cos(azimuth * Mathf.Deg2Rad) +
            ortho2 * Mathf.Sin(azimuth * Mathf.Deg2Rad);

        Vector3 offsetDir =
            (Quaternion.AngleAxis(angle, spreadAxis) * baseDir).normalized;

        Quaternion spawnRot = Quaternion.LookRotation(offsetDir);

        SpawnLightning(spawnPos, offsetDir);
        AfterFire();
    }

    void SpawnLightning(Vector3 startPos, Vector3 direction)
    {
        Ray ray = new Ray(startPos, direction);
        if (Physics.Raycast(ray, out RaycastHit hit, maxLength))
        {
            for (int i = 0; i < lightningAmount; i++)
            {
                LineRenderer lr = Instantiate(lightningPrefab, transform).GetComponent<LineRenderer>();
                lr.gameObject.SetActive(true);
                GenerateLightningBolt(lr, startPos, hit.point);
                ApplyDamage(hit);
            }
        }
        else
        {
            Vector3 endPos = startPos + direction * maxLength;
            for (int i = 0; i < lightningAmount; i++)
            {
                LineRenderer lr = Instantiate(lightningPrefab, transform).GetComponent<LineRenderer>();
                lr.gameObject.SetActive(true);
                GenerateLightningBolt(lr, startPos, endPos);
            }
        }
    }

    void GenerateLightningBolt(LineRenderer lr, Vector3 start, Vector3 end)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        int segments = Mathf.Min(maxSegments, Mathf.FloorToInt(distance / segmentLength));

        lr.positionCount = segments + 1;
        lr.SetPosition(0, start);

        Vector3 currentPos = start;
        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 targetPos = Vector3.Lerp(start, end, t);

            Vector3 perp1 = Vector3.Cross(direction, Vector3.up).normalized;
            Vector3 perp2 = Vector3.Cross(direction, perp1).normalized;

            float noise1 = (ClassicRandom.value - 0.5f) * displacement;
            float noise2 = (ClassicRandom.value - 0.5f) * displacement;

            currentPos = targetPos + perp1 * noise1 + perp2 * noise2;
            lr.SetPosition(i, currentPos);

            direction = (currentPos - lr.GetPosition(i - 1)).normalized;
        }

        lr.SetPosition(segments, end);

        StartCoroutine(FadeLightning(lr));
    }

    System.Collections.IEnumerator FadeLightning(LineRenderer lr)
    {
        float timer = 0;
        Color startColor = lr.material.color;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, timer / fadeTime);
            lr.material.color = newColor;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    void ApplyDamage(RaycastHit hit)
    {
        switch (hit.collider.tag)
        {
            case "Enemy":
                var enemy = hit.collider.GetComponent<Enemy>();
                if (isStealthShot && !enemy.enemyAwareness.isAggro) enemy.Die();
                else enemy.TakeDamage(damage);
                enemy.StartCoroutine(DamageEnemyCor(enemy));
                break;
            
            case "Player":
                hit.collider.GetComponent<PlayerHealth>().DamagePlayer(damage);
                break;

            case "ExplosiveObject":
                var barrel = hit.collider.GetComponent<RedBarrel>();
                if (barrel != null)
                {
                    barrel.health -= damage;
                    if (barrel.health <= 0) barrel.Boom();
                }
                break;

            case "Environment":
                break;

            default:
                break;
        }
    }
}
