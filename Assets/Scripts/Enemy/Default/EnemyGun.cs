using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

public class EnemyGun : MonoBehaviour
{
    public float lastShotTime;
    protected float reloadTime = 0;
    protected short minDamage = 1;
    protected short maxDamage = 5;
    protected float minDelayShootTime = 0.7f;
    protected float maxDelayShootTime = 1f;
    public AudioSource audioSource;
    public AudioClip shotSound;
    public EnemyAi enemyAi;
    public Light light;
    private float maxLightIntensity;

    private float hitChance = 0.7f;
    public float shootingDistance = 50f;

    public bool isReadyToShoot;
    public byte grenadeCount = 1;
    public static bool isSmokeGrenadeUsable = true;
    public static bool isEmpGrenadeUsable = false;

    public short damage = 2;

    public GameObject projectilePrefab;
    public void Start()
    {
        enemyAi = GetComponentInParent<EnemyAi>();
        lastShotTime = -maxDelayShootTime;
        isReadyToShoot = true;
        reloadTime = GetRandomDelayTime();
        maxLightIntensity = light.intensity;

        minDamage = 15;
        maxDamage = 20;

    }

    public virtual void Update()
    {
        if (enemyAi.isPlayerSpotted && !enemyAi.isMeleeAttacking && enemyAi.enemyAwareness.isAggro)
        {
            Collider playerCollider = FindPlayerCollider();
            if (playerCollider != null
                && IsPlayerInShootingRange(playerCollider)
                && Time.time - lastShotTime >= GetRandomDelayTime()
                && isReadyToShoot)
            {
                if (CanShootAtPlayer(playerCollider))
                {
                    StartCoroutine(ShootAtPlayer(playerCollider, ClassicRandom.Range(3,6)));
                }
            }
        }
    }

    protected Collider FindPlayerCollider()
    {
        return PlayerController.Instance.GetComponent<Collider>();
    }

    public bool IsPlayerInShootingRange(Collider target)
    {
        float distanceToPlayer = Vector3.Distance(transform.position, target.transform.position);
        return distanceToPlayer <= shootingDistance;
    }

    public float GetDistanceToPlayer()
    {
        return Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
    }

    public bool CanShootAtPlayer(Collider target)
    {
        Vector3 origin = transform.position;
        Vector3 directionToPlayer = (target.transform.position - origin).normalized;
        float distanceToPlayer = Vector3.Distance(origin, target.transform.position);

        if (Physics.Raycast(origin, directionToPlayer, out RaycastHit hit, distanceToPlayer))
        {
            if (hit.collider.CompareTag("Player")) return true;
        }

        return false;
    }

    public virtual IEnumerator ShootAtPlayer(Collider target, int amount)
    {
        if(PlayerController.Instance.playerHealth.isDead) yield break;
        
        isReadyToShoot = false;

        yield return new WaitForSeconds(ClassicRandom.value);

        for (int i = 0; i < amount; i++)
        {

            var waitTime = ClassicRandom.value / 2;

            StartCoroutine(LookAtPlayer(target, waitTime));

            Vector3 direction = (target.transform.position - transform.position).normalized;
            Quaternion spawnRot = Quaternion.LookRotation(direction);

            Projectile.Spawn(projectilePrefab, transform.position, spawnRot, gameObject);

            audioSource.PlayOneShot(shotSound);

            lastShotTime = Time.time;
            yield return new WaitForSeconds(waitTime);
        }

        isReadyToShoot = true;
    }

    public IEnumerator LookAtPlayer(Collider target, float time = 1f)
    {

        float elapsed = 0f;

        while (elapsed < time)
        {
            if (target == null) yield break;
            if(!enemyAi) yield break;

            Vector3 direction = (target.transform.position - transform.position).normalized;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion bodyRot = Quaternion.LookRotation(direction);
                enemyAi.transform.rotation = Quaternion.Slerp(
                    enemyAi.transform.rotation, bodyRot, Time.deltaTime * 15f);
            }

            transform.LookAt(target.transform.position);

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    protected float GetRandomDelayTime()
    {
        return ClassicRandom.Range(minDelayShootTime, maxDelayShootTime);
    }
    public IEnumerator EnableLight()
    {
        float currentLightIntensity = maxLightIntensity;
        light.enabled = true;
        light.intensity = maxLightIntensity;
        yield return new WaitForSeconds(0.1f);

        while (light.intensity > 0)
        {
            currentLightIntensity -= maxLightIntensity / 3;
            light.intensity = currentLightIntensity;
            yield return new WaitForSeconds(0.03f);
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
        light.enabled = false;
    }
}