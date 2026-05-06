using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyAwareness : MonoBehaviour
{
    public float awarenessRadius = 50f;
    private float fieldOfView = 140f;
    protected float friendDetectField = 100;
    public bool isAggro;
    public bool playerVisible = false;
    protected static Transform playersTransform;
    public List<EnemyAwareness> nearbyEnemies = new List<EnemyAwareness>();
    public bool isStunned;
    private Coroutine awarenessCoroutine;
    private Coroutine stunCoroutine;

    private const float spotTime = 1f;
    private static float spotElapsedTime = 0f;
    private static float lostSightTime = 0f;
    private const float lostSightThreshold = 5f;

    // --- Тени: пороги тьмы ---
    private const float SHADOW_INVISIBLE_THRESHOLD = 0.95f; // выше → невидим для нормального FOV
    private const float SHADOW_HEAVY_THRESHOLD     = 0.75f; // скорость обнаружения / 2
    private const float SHADOW_MEDIUM_THRESHOLD    = 0.50f; // скорость обнаружения / 1.5

    // --- Тени: модификаторы скорости (применяются к accumSpeed) ---
    private const float SHADOW_HEAVY_SPEED_FACTOR  = 0.5f;          // в 2 раза медленнее
    private const float SHADOW_MEDIUM_SPEED_FACTOR = 1f / 1.5f;     // в 1.5 раза медленнее

    // --- Обнаружение в тени: узкий конус + близкое расстояние ---
    // Когда тьма > SHADOW_INVISIBLE_THRESHOLD:
    private const float DEEP_SHADOW_CLOSE_RANGE    = 6f;   // до 3 ед. — видят почти вплотную
    private const float DEEP_SHADOW_ACCUM_SPEED    = 4f;   // spotTime=1 / 2 = 0.5 сек

    // При любом уровне тени, но прямо перед противником и близко:
    private const float NARROW_CLOSE_RANGE         = 5f;   // "короткое расстояние"
    private const float NARROW_CLOSE_ACCUM_SPEED   = 1f;   // ровно 1 секунда
    private const float NARROW_FOV_HALF_ANGLE      = 5f;   // половина 10-градусного конуса

    // Обнаружение по близости — без проверки угла, неважно спереди или сзади:
    private const float PROXIMITY_RANGE            = 3f;   // в пределах 3 ед.
    private const float PROXIMITY_ACCUM_SPEED      = 1f;   // ровно 1 секунда

    private static AudioSource spotNoise;
    public AudioClip spotSound;

    private StealthShadowDetector playerStealthShadowDetector;

    void Start()
    {
        if (playersTransform == null)
        {
            playersTransform = PlayerController.Instance.transform;
        }
        playerStealthShadowDetector = PlayerController.Instance.GetStealthShadowDetector();
        awarenessCoroutine = StartCoroutine(CheckAwareness());
    }

    public async Task GetNearbyEnemies(float maxDistance)
    {
        await Awaitable.MainThreadAsync();

        nearbyEnemies.Clear();
        Vector3 scanOrigin = transform.position;

        int obstacleMask = LayerMask.GetMask("Default", "Environment");

        Collider[] hits = Physics.OverlapSphere(scanOrigin, maxDistance);
        foreach (var hit in hits)
        {
            if (hit.transform == transform) continue;

            EnemyAwareness enemy = hit.GetComponent<EnemyAwareness>();
            if (enemy != null)
            {
                Vector3 direction = (enemy.transform.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, enemy.transform.position);

                RaycastHit raycastHit;
                if (Physics.Raycast(transform.position, direction, out raycastHit, distance + 0.5f, obstacleMask))
                {
                    if (raycastHit.transform == enemy.transform)
                    {
                        nearbyEnemies.Add(enemy);
                    }
                }
                else
                {
                    nearbyEnemies.Add(enemy);
                }
            }
        }
    }

    public virtual IEnumerator CheckAwareness()
    {
        while (!isAggro)
        {
            if (isStunned)
            {
                playerVisible = false;
                yield return new WaitForEndOfFrame();
                continue;
            }

            // Дым блокирует обнаружение, если игрок в дыму, а враг — нет
            if (SmokeGrenadeEffectRunner.IsPositionInSmoke(playersTransform.position) &&
                !SmokeGrenadeEffectRunner.IsPositionInSmoke(transform.position))
            {
                playerVisible = false;
                yield return new WaitForEndOfFrame();
                continue;
            }

            playerVisible = false;

            float darkness  = playerStealthShadowDetector.darknessLevel;
            Vector3 toPlayer = playersTransform.position - transform.position;
            float sqrDist   = toPlayer.sqrMagnitude;
            float distance  = Mathf.Sqrt(sqrDist);
            float angle     = Vector3.Angle(transform.forward, toPlayer);

            bool deepShadow  = darkness >= SHADOW_INVISIBLE_THRESHOLD;
            bool inNarrowFov = angle < NARROW_FOV_HALF_ANGLE;

            // Режим 1: близость — без проверки угла, спереди или сзади, 1 сек
            bool proximityDetect = distance <= PROXIMITY_RANGE;

            // Режим 2: глубокая тень (>0.95) — только вплотную и прямо, 0.5 сек
            bool deepShadowDetect = !proximityDetect
                                 && deepShadow
                                 && distance <= DEEP_SHADOW_CLOSE_RANGE
                                 && inNarrowFov;

            // Режим 3: узкий конус + короткая дистанция — всегда 1 сек, без штрафа тени
            bool narrowCloseDetect = !proximityDetect
                                  && !deepShadow
                                  && distance <= NARROW_CLOSE_RANGE
                                  && inNarrowFov;

            // Режим 4: нормальный FOV — только если не в глубокой тени
            bool normalDetect = !proximityDetect
                             && !deepShadow
                             && sqrDist < awarenessRadius * awarenessRadius
                             && angle < fieldOfView / 2f;

            if (proximityDetect || deepShadowDetect || narrowCloseDetect || normalDetect)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, toPlayer.normalized, out hit, awarenessRadius))
                {
                    if (hit.transform == playersTransform)
                    {
                        float accumSpeed;

                        if (proximityDetect)
                        {
                            // 1 секунда, угол не важен
                            accumSpeed = PROXIMITY_ACCUM_SPEED;
                        }
                        else if (deepShadowDetect)
                        {
                            // 0.5 секунды, без учёта дистанции
                            accumSpeed = DEEP_SHADOW_ACCUM_SPEED;
                        }
                        else if (narrowCloseDetect)
                        {
                            // Ровно 1 секунда, теневой штраф не применяется
                            accumSpeed = NARROW_CLOSE_ACCUM_SPEED;
                        }
                        else
                        {
                            // Нормальное обнаружение: чем ближе — тем быстрее,
                            // плюс штраф от тьмы
                            float distanceFactor = 1f - Mathf.Clamp01(distance / awarenessRadius);
                            float shadowFactor   = GetShadowSpeedFactor(darkness);
                            accumSpeed = Mathf.Lerp(0.3f, 2f, distanceFactor) * shadowFactor;
                        }

                        playerVisible = true;
                        lostSightTime = 0f;
                        spotElapsedTime += Time.deltaTime * accumSpeed;
                        spotElapsedTime  = Mathf.Clamp(spotElapsedTime, 0f, spotTime);

                        SyncSpotNoise(spotElapsedTime);

                        if (spotElapsedTime >= spotTime)
                        {
                            GetComponent<AudioSource>().PlayOneShot(spotSound);
                            EnemyManager.AggroAll();
                            ShaderVolumeManager.Instance.SetFilmGrainByValue(0);
                            Destroy(spotNoise.gameObject);
                        }
                    }
                }
            }

            if (!playerVisible)
            {
                lostSightTime += Time.deltaTime;
                if (lostSightTime >= lostSightThreshold && spotElapsedTime > 0f)
                {
                    spotElapsedTime -= Time.deltaTime;
                    spotElapsedTime  = Mathf.Max(spotElapsedTime, 0f);
                    SyncSpotNoise(spotElapsedTime);
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    // Возвращает множитель скорости накопления для нормального FOV-обнаружения.
    // < 1 = медленнее (игрок в тени), 1 = нормально.
    private float GetShadowSpeedFactor(float darkness)
    {
        if      (darkness >= SHADOW_HEAVY_THRESHOLD)  return SHADOW_HEAVY_SPEED_FACTOR;
        else if (darkness >= SHADOW_MEDIUM_THRESHOLD) return SHADOW_MEDIUM_SPEED_FACTOR;
        else                                          return 1f;
    }

    public void TriggerNearbyEnemies()
    {
        if (!isAggro) return;

        _ = GetNearbyEnemies(friendDetectField).ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError(t.Exception);
            }
        }, TaskScheduler.Default);

        foreach (var enemy in nearbyEnemies)
        {
            enemy.isAggro = true;
        }
    }

    public static void SyncSpotNoise(float spotTime)
    {
        if (!spotNoise)
        {
            var obj = new GameObject("SpotNoise");
            spotNoise = obj.AddComponent<AudioSource>();
            spotNoise.volume = 0f;
            spotNoise.loop = true;
            spotNoise.clip = Resources.Load<AudioClip>("SFX/DeathNoisePad");
            spotNoise.Play();
        }

        spotNoise.volume = spotTime;
        ShaderVolumeManager.Instance.SetFilmGrainByValue(spotTime);
    }

    // Оставлен для обратной совместимости если где-то используется извне
    public float GetPlayerShadowDebuff()
    {
        return GetShadowSpeedFactor(playerStealthShadowDetector.darknessLevel);
    }

    public void Stun(float duration)
    {
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunCoroutine(duration));
    }

    private IEnumerator StunCoroutine(float duration)
    {
        isStunned = true;
        playerVisible = false;
        yield return new WaitForSeconds(duration);
        isStunned = false;
        stunCoroutine = null;
    }

    public void DeAggro()
    {
        isAggro = false;
        playerVisible = false;
        spotElapsedTime = 0f;
        lostSightTime = 0f;
        SyncSpotNoise(0f);
        if (awarenessCoroutine != null) StopCoroutine(awarenessCoroutine);
        awarenessCoroutine = StartCoroutine(CheckAwareness());
        EnemyFSM.CheckAllDeAggro();
    }
}
