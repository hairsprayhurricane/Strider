using UnityEngine;
using System.Collections;

public class FlashBangEffectRunner : MonoBehaviour
{
    public void Begin(int lightCount, float radius, float maxLightIntensity,
        float fadeDuration, float maxGamma, float gammaDuration, float delay,
        float smokeSize, Color smokeColor,
        float enemyStunRadius, float enemyStunDuration, Vector3 spawnPosition)
    {
        StartCoroutine(Run(lightCount, radius, maxLightIntensity, fadeDuration,
            maxGamma, gammaDuration, delay, smokeSize, smokeColor,
            enemyStunRadius, enemyStunDuration, spawnPosition));
    }

    private IEnumerator Run(int lightCount, float radius, float maxIntensity,
        float fadeDuration, float maxGamma, float gammaDuration, float delay,
        float smokeSize, Color smokeColor,
        float enemyStunRadius, float enemyStunDuration, Vector3 spawnPosition)
    {
        yield return new WaitForSeconds(delay);

        Vector3 center = spawnPosition;

        Collider[] stunBuffer = new Collider[32];
        int hitCount = Physics.OverlapSphereNonAlloc(center, enemyStunRadius, stunBuffer);
        for (int s = 0; s < hitCount; s++)
        {
            if (stunBuffer[s].TryGetComponent(out EnemyAi enemyAi))
                enemyAi.Stun(enemyStunDuration);
        }

        for (int i = 0; i < lightCount; i++)
        {
            float angle   = Random.Range(0f, Mathf.PI * 2f);
            float dist    = Random.Range(0f, radius);
            float yOffset = Random.Range(-0.3f, 0.3f);
            Vector3 offset = new(Mathf.Cos(angle) * dist, yOffset, Mathf.Sin(angle) * dist);

            var go = new GameObject("FlashBangLight");
            go.transform.position = center + offset;
            var l = go.AddComponent<Light>();
            l.intensity = 0f;
            l.range     = radius * 4f;


            l.color = new Color(r: 1, g: Random.value/2, b: 0);

            StartCoroutine(LightFlicker(l, maxIntensity, fadeDuration));
        }

        if (ShaderVolumeManager.Instance != null)
            ShaderVolumeManager.Instance.TriggerGammaFlash(maxGamma, gammaDuration);

        // Smoke fades out at light speed — fadeOutDuration tied to fadeDuration
        SmokeCloud.Spawn(center, smokeSize, fadeDuration, smokeColor);

        yield return new WaitForSeconds(fadeDuration + 1f);
        Destroy(gameObject);
    }

    private IEnumerator LightFlicker(Light light, float maxIntensity, float fadeDuration)
    {
        int   flickerCount   = Random.Range(3, 6);
        float flickerOnTime  = 0.04f;
        float flickerOffTime = 0.03f;

        for (int i = 0; i < flickerCount; i++)
        {
            float t = 0f;
            while (t < flickerOnTime)
            {
                t += Time.deltaTime;
                light.intensity = Mathf.Lerp(0f, maxIntensity, t / flickerOnTime);
                yield return null;
            }
            light.intensity = maxIntensity;

            t = 0f;
            while (t < flickerOffTime)
            {
                t += Time.deltaTime;
                light.intensity = Mathf.Lerp(maxIntensity, 0f, t / flickerOffTime);
                yield return null;
            }
            light.intensity = 0f;
        }

        float fade = 0f;
        while (fade < fadeDuration)
        {
            fade += Time.deltaTime;
            light.intensity = Mathf.Lerp(maxIntensity, 0f, fade / fadeDuration);
            yield return null;
        }

        Destroy(light.gameObject);
    }
}
