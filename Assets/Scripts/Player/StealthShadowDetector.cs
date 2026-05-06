using System.Collections.Generic;
using UnityEngine;

public class StealthShadowDetector : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask occlusionMask = Physics.DefaultRaycastLayers;

    public int updateEveryNFrames = 3;

    public float sampleHeight = 0.9f;

    public float smoothSpeed = 111f;

    [Header("State (read-only)")]
    [Range(0f, 1f)] public float darknessLevel;      // 1 = полная тень, 0 = полный свет
    [Range(0f, 1f)] public float forcedDarknessLevel; // устанавливается внешне (дым и т.п.), плавно работает
    Light _mainLight;
    Light[] _additionalLights;
    int _frameCounter;
    int _lightsRefreshFrame;

    // Раз в 120 кадров обновляем список ламп на случай их появления/удаления
    const int LightsRefreshInterval = 120;

    void Start() => RefreshLights();

    void RefreshLights()
    {
        _mainLight = RenderSettings.sun;

        var all = FindObjectsByType<Light>(FindObjectsSortMode.None);
        var additional = new List<Light>(all.Length);
        foreach (var l in all)
            if (l != _mainLight && l.type != LightType.Directional)
                additional.Add(l);
        _additionalLights = additional.ToArray();
    }

    void Update()
    {
        if (forcedDarknessLevel >= 1f)
        {
            darknessLevel = 1f;
            ShaderVolumeManager.Instance.SetVignetteIntensity(0.5f);
            return;
        }

        ShaderVolumeManager.Instance.SetVignetteIntensity(darknessLevel / 2);

        _lightsRefreshFrame++;
        if (_lightsRefreshFrame >= LightsRefreshInterval)
        {
            _lightsRefreshFrame = 0;
            RefreshLights();
        }

        _frameCounter++;
        if (_frameCounter % updateEveryNFrames != 0) return;

        Vector3 samplePoint = transform.position + Vector3.up * sampleHeight;

        // Исключаем слой самого игрока, чтобы рейкаст не бил в собственный коллайдер
        int castMask = occlusionMask & ~(1 << gameObject.layer);

        // Тьма начинается с 1 — каждый источник вычитает свой вклад
        float rawDarkness = 1f;

        if (_mainLight != null && _mainLight.enabled && _mainLight.gameObject.activeInHierarchy)
        {
            Vector3 toLightDir = -_mainLight.transform.forward;
            bool blocked = Physics.Raycast(samplePoint, toLightDir, Mathf.Infinity, castMask);
            if (!blocked)
            {
                float contrib = Mathf.Clamp01(_mainLight.intensity) *
                                Mathf.Clamp01(_mainLight.color.maxColorComponent);
                rawDarkness -= contrib;
            }
        }

        // --- Дополнительные источники (Point / Spot) ---
        foreach (var light in _additionalLights)
        {
            if (light == null || !light.enabled || !light.gameObject.activeInHierarchy) continue;

            Vector3 toLight  = light.transform.position - samplePoint;
            float   distance = toLight.magnitude;

            if (distance > light.range) continue;

            // Spot: проверяем попадание в конус
            if (light.type == LightType.Spot)
            {
                float angle = Vector3.Angle(-light.transform.forward, toLight.normalized);
                if (angle > light.spotAngle * 0.5f) continue;
            }

            // Геометрическое перекрытие
            if (Physics.Raycast(samplePoint, toLight.normalized, distance, castMask)) continue;

            // Квадратичное затухание по расстоянию + интенсивность источника
            float t       = 1f - Mathf.Clamp01(distance / light.range);
            float contrib = t * t * Mathf.Clamp01(light.intensity / 4f) *
                            Mathf.Clamp01(light.color.maxColorComponent);
            rawDarkness -= contrib;
        }

        float target = Mathf.Max(Mathf.Clamp01(rawDarkness), forcedDarknessLevel);
        darknessLevel = Mathf.Clamp01(Mathf.Lerp(darknessLevel, target, Time.deltaTime * smoothSpeed));
    }

    /*void OnDrawGizmosSelected()
    {
        Gizmos.color = isHidden ? Color.blue : Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.8f, 0.1f, 0.8f));

        Gizmos.color = Color.Lerp(Color.yellow, Color.black, darknessLevel);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
    }*/
}
