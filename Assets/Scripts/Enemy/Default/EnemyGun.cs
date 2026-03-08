using UnityEngine;
using System.Collections;

public class EnemyGun : MonoBehaviour
{
    public float lastShotTime;
    protected float reloadTime = 0;
    protected short minDamage = 1;
    protected short maxDamage = 5;
    protected float minDelayShootTime = 0.7f;
    protected float maxDelayShootTime = 1f;
    public AudioSource audioSource;
    public EnemyAi enemyAi;
    public Light light;
    private float maxLightIntensity;

    private float hitChance = 0.7f;
    public float shootingDistance = 50f;

    public bool isReadyToShoot;
    public byte grenadeCount = 1;
    public static bool isSmokeGrenadeUsable = true;
    public static bool isEmpGrenadeUsable = false;
    public void Start()
    {
        enemyAi = GetComponentInParent<EnemyAi>();
        lastShotTime = -maxDelayShootTime;
        isReadyToShoot = true;
        reloadTime = GetRandomDelayTime();
        maxLightIntensity = light.intensity;

        minDamage = 15;
        maxDamage = 30;
    }


    public virtual void Update()
    {
        if (enemyAi.isPlayerSpotted && !enemyAi.isMeleeAttacking && enemyAi.enemyAwareness.isAggro)
        {
            Collider playerCollider = FindPlayerCollider();
            if (playerCollider != null && IsPlayerInShootingRange(playerCollider) && Time.time - lastShotTime >= GetRandomDelayTime() && isReadyToShoot)
            {
                if (CanShootAtPlayer(playerCollider))
                {
                    ShootAtPlayer(playerCollider);
                    StartCoroutine(ResetShootingAnimation(reloadTime));
                    
                }
            }

        }
        //Debug.DrawRay(transform.position, transform.forward * 10000, Color.red);
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
        float distanceToPlayer = Vector3.Distance(transform.position, PlayerController.Instance.transform.position);
        return distanceToPlayer;
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

/*    protected virtual IEnumerator ShootAnimCor()
    {
        animator.SetBool("isShooting", true);
        animator.Play("enemyShoot1");
        yield return new WaitForSeconds(0.3f);
        animator.Play("enemyShoot2");
        StartCoroutine(EnableLight());
        yield return new WaitForSeconds(1);
        animator.SetBool("isShooting", false);
    }
*/
    public virtual void ShootAtPlayer(Collider target)
    {
        transform.LookAt(target.transform.position);

        if (HitTarget())
        {
            DamagePlayer(target);
        }

        if (!audioSource.isPlaying)
        {
            //StartCoroutine(ShootAnimCor());
            audioSource.Play();
        }

        lastShotTime = Time.time;

        isReadyToShoot = false;
    }

    protected bool HitTarget()
    {
        return ClassicRandom.value <= hitChance;
    }

    protected void DamagePlayer(Collider target)
    {
        PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            short damage = ClassicRandom.RangeShort(minDamage, maxDamage);
            playerHealth.DamagePlayer(damage, transform);
        }
    }

    protected float GetRandomDelayTime()
    {
        return ClassicRandom.Range(minDelayShootTime, maxDelayShootTime);
    }

    public virtual IEnumerator ResetShootingAnimation(float time)
    {
        yield return new WaitForSeconds(time);
        isReadyToShoot = true;
    }

    public virtual IEnumerator ResetShootingAnimation()
    {
        yield return new WaitForSeconds(reloadTime);
        isReadyToShoot = true;
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
