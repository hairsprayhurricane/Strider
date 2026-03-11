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

    private const byte spotTime = 1;
    private static float spotElapsedTime = 0f;
    private static float lostSightTime = 0f;
    private const float lostSightThreshold = 5f;

    private static AudioSource spotNoise;
    public AudioClip spotSound;

    void Start()
    {
        if (playersTransform == null)
        {
            playersTransform = PlayerController.Instance.transform;
        }
        //GetNearbyEnemies(friendDetectField);
        StartCoroutine(CheckAwareness());
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
            playerVisible = false;
            Vector3 toPlayer = playersTransform.position - transform.position;
            float sqrDist = toPlayer.sqrMagnitude;

            if (sqrDist < awarenessRadius * awarenessRadius)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer);
                if (angle < fieldOfView / 2f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, toPlayer.normalized, out hit, awarenessRadius))
                    {
                        if (hit.transform == playersTransform)
                        {
                            float distance = Mathf.Sqrt(sqrDist);
                            float distanceFactor = 1f - Mathf.Clamp01(distance / awarenessRadius);
                            float accumSpeed = Mathf.Lerp(0.3f, 2f, distanceFactor);

                            playerVisible = true;
                            lostSightTime = 0f;
                            spotElapsedTime += Time.deltaTime * accumSpeed;
                            spotElapsedTime = Mathf.Clamp(spotElapsedTime, 0f, spotTime);

                            SyncSpotNoise(spotElapsedTime);
                            //ShaderVolumeManager.Instance.SetFilmGrainByValue(spotElapsedTime);

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
            }

            if (!playerVisible)
            {
                lostSightTime += Time.deltaTime;
                if (lostSightTime >= lostSightThreshold && spotElapsedTime > 0f)
                {
                    spotElapsedTime -= Time.deltaTime;
                    spotElapsedTime = Mathf.Max(spotElapsedTime, 0f);
                    //ShaderVolumeManager.Instance.SetFilmGrainByValue(spotElapsedTime);
                    SyncSpotNoise(spotElapsedTime);
                }
            }

            //TriggerNearbyEnemies();
            yield return new WaitForEndOfFrame();
        }
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
            //if (PlayerController.isInvisible) isAggro = false;
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
}
