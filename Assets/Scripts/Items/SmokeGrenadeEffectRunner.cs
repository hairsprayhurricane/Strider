using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class SmokeGrenadeEffectRunner : MonoBehaviour
{
    private static readonly List<SmokeGrenadeEffectRunner> _activeRunners = new();

    private Vector3 _center;
    private float   _radius;

    public static bool IsPositionInSmoke(Vector3 pos)
    {
        foreach (var r in _activeRunners)
            if (r != null && Vector3.Distance(pos, r._center) <= r._radius) return true;
        return false;
    }

    public void Begin(float radius, float duration, float smokeSize, Color smokeColor, int cloudCount, Vector3 spawnPosition)
    {
        StartCoroutine(Run(radius, duration, smokeSize, smokeColor, cloudCount, spawnPosition));
    }

    private IEnumerator Run(float radius, float duration, float smokeSize, Color smokeColor, int cloudCount, Vector3 spawnPosition)
    {
        _center = spawnPosition;
        _radius = radius;
        _activeRunners.Add(this);

        StealthShadowDetector shadowDetector = PlayerController.Instance != null
            ? PlayerController.Instance.GetStealthShadowDetector()
            : null;
        Transform playerTransform = PlayerController.Instance != null
            ? PlayerController.Instance.transform
            : null;

        StartCoroutine(SpawnCloudsStaggered(_center, cloudCount, radius, smokeSize, duration, smokeColor));

        var confusedEnemies  = new HashSet<EnemyAi>();
        var buffer           = new Collider[32];
        const float pollInterval = 0.3f;
        float holdDuration   = duration * 0.75f;
        float fadeDuration   = duration * 0.25f;
        float elapsed        = 0f;

        while (elapsed < duration)
        {
            float smokeOpacity = elapsed < holdDuration
                ? 1f
                : 1f - (elapsed - holdDuration) / fadeDuration;

            if (playerTransform != null && shadowDetector != null)
            {
                bool playerInSmoke = Vector3.Distance(playerTransform.position, _center) <= radius;
                shadowDetector.forcedDarknessLevel = playerInSmoke ? smokeOpacity : 0f;
            }

            if (smokeOpacity > 0.2f)
            {
                int hitCount = Physics.OverlapSphereNonAlloc(_center, radius, buffer);
                for (int i = 0; i < hitCount; i++)
                {
                    if (!buffer[i].TryGetComponent(out EnemyAi ai)) continue;
                    if (confusedEnemies.Contains(ai)) continue;
                    confusedEnemies.Add(ai);
                    ai.WanderInArea(_center, radius, duration - elapsed);
                }
            }

            yield return new WaitForSeconds(pollInterval);
            elapsed += pollInterval;
        }

        if (shadowDetector != null) shadowDetector.forcedDarknessLevel = 0f;
        _activeRunners.Remove(this);
        Destroy(gameObject);
    }

    private IEnumerator SpawnCloudsStaggered(Vector3 center, int count, float radius,
        float smokeSize, float duration, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * radius * 0.75f;
            offset.y = Random.Range(-0.2f, 0.5f);
            SpawnCloud(center + offset, smokeSize, duration, color);
            yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
        }
    }

    private static void SpawnCloud(Vector3 position, float size, float duration, Color color)
    {
        var go                = new GameObject("SmokeGrenadeCloud");
        go.transform.position = position;
        var cloud             = go.AddComponent<SmokeCloud>();
        cloud.persistent      = true;
        cloud.growDuration    = Random.Range(0.25f, 0.45f);
        cloud.size            = size;
        cloud.cloudColor      = color;
        cloud.lifetime        = duration;
        cloud.fadeInDuration  = 0.4f;
        cloud.fadeOutDuration = Mathf.Min(2.5f, duration * 0.25f);
    }

    private void OnDestroy()
    {
        _activeRunners.Remove(this);
    }
}
