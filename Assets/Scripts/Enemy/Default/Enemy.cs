using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.Universal;

public class Enemy : MonoBehaviour
{
    private EnemyType enemyType;
    private AngleToPlayer angleToPlayer;

    [HideInInspector] public EnemyAwareness enemyAwareness;
    [HideInInspector] public EnemyAi enemyAi;
    public EnemyGun enemyGun;
    [HideInInspector] public EnemyMelee enemyMelee;

    private Rigidbody rb;
    private static GameObject redSquarePrefab;
    public static GameObject bloodPuddlePrefab;
    public AudioSource audioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;

    public short enemyHealth = 200;
    public static byte supplyDropChance = 99;
    public bool isDead;
    public static bool CensorMode;
    public bool isHealthInitialized = false;
    public byte diedBy = 0;
    public bool isDroppedSupply = false;
    [HideInInspector] public Collider collider;
    public Collider physicalCollider;
    private static int nextDropKillTarget = 0;
    public Light headLight;
    public enum DeathReason
    {
        body = 1,
        head = 2,
        // not in use
        explosion = 3,
        fire = 4,
        electrisity = 5
    }

    private GameObject rayObject;
    private LineRenderer rayLine;

    public void Start()
    {
        rb = GetComponent<Rigidbody>();
        enemyAi = GetComponent<EnemyAi>();
        enemyType = GetComponentInParent<EnemyType>();
        enemyMelee = GetComponentInChildren<EnemyMelee>();
        enemyAwareness = GetComponent<EnemyAwareness>();
        angleToPlayer = GetComponent<AngleToPlayer>();

        rb.linearDamping = 1f;

        enemyAwareness = GetComponent<EnemyAwareness>();

        collider = GetComponent<Collider>();

        redSquarePrefab = Resources.Load<GameObject>("Prefabs/Enemies/Gore/EnemyBloodCell");
        bloodPuddlePrefab = Resources.Load<GameObject>("Prefabs/Enemies/Gore/BloodPuddle");

        //InitializeHealth();

        //KelerMarker.Instance.CallKelerOnEnemy(this, 1);
    }

    void Update()
    {
        if(!isDead) DrawRay(transform.position, transform.forward * 10);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Projectile") && other.GetComponent<Projectile>().isShootedByPlayer)
        {
            _ = SpawnRedSquaresAsync(5).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    Debug.LogError(t.Exception);
                }
            }, TaskScheduler.Default);
        }
    }

    public void CheckForDeath(byte deadBy)
    {
        //Debug.Log(nextDropKillTarget);
        if (enemyHealth <= 0 && !isDead)
        {
            /*globalKillCounter++;

            if (globalKillCounter >= nextDropKillTarget)
            {
                globalKillCounter = 0;
                RollNextDropTarget();
                CreateSupply();
            }*/

            Die(deadBy);
        }
    }

    public void Die(byte deadBy = 1, bool spawnPuddle = true)
    {
        if (CensorMode)
        {
            gameObject.GetComponentInChildren<SpriteRenderer>().color = Color.blue;
        }
        diedBy = deadBy;
        enemyHealth = 0;
        //spriteAnim.SetBool("isDead", true);

        if (deadBy == 2)
        {
            //enemyType.points += 20;
        }
        //enemyType.givePoints();

        enemyType.isDied = true;

        if (deadBy == 1)
        {
            //spriteAnim.Play("EnemyDie1");
        }
        else if (deadBy == 2)
        {
            //spriteAnim.Play("EnemyDie2");
        }
        else
        {
            //spriteAnim.Play("EnemyDie1");
        }

        if (collider is CapsuleCollider capsuleCollider)
        {
            var center = capsuleCollider.center;
            center.y -= 0.45f;
            capsuleCollider.center = center;
        }

        physicalCollider.enabled = false;

        enemyGun.light.gameObject.SetActive(false);

        if (enemyAi != null)
        {
            enemyAi.StopAllCoroutines();
            enemyAi.ResetPathForNav();
            Destroy(enemyAi);
        }

        if (enemyGun) enemyGun.StopAllCoroutines();

        Destroy(enemyGun);
        Destroy(enemyAwareness);
        if (spawnPuddle)
        {
            SpawnBloodPuddle(transform.position);
        }

        GetComponentInChildren<Renderer>().transform.position += new Vector3(0, -1.5f, 0);

        isDead = true;
        //Counters.killCount++;

        audioSource.PlayOneShot(deathSound);

        Destroy(rayObject);

        EnemyFSM.CheckAllCleared();
    }
    public bool TakeDamage(short damage)
    {
        if (!enemyAwareness.isAggro)
        {
            enemyAwareness.isAggro = true;
        }

        //enemyAwareness.TriggerNearbyEnemies();
        //EnemyManager.AggroAll();
        enemyHealth -= damage;

        _ = SpawnRedSquaresAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                Debug.LogError(t.Exception);
            }
        }, TaskScheduler.Default);

        
        audioSource.PlayOneShot(hitSound);

        CheckForDeath((int)Enemy.DeathReason.body);

        if (enemyHealth < 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public async Task SpawnRedSquaresAsync(int count = 10, float force = 20, bool spawnInHead = false)
    {
        await Awaitable.MainThreadAsync();

        Vector3 spawnPosition;
        if (spawnInHead)
        {
            spawnPosition = headLight.transform.position;
        }
        else
        {
            spawnPosition = transform.position;
        }

        for (int i = 0; i < count; i++)
        {
            //Counters.enemyBloodSpilled++;
            GameObject redSquare = Instantiate(
                redSquarePrefab,
                spawnPosition,
                Quaternion.identity,
                transform.parent
            );

            if (CensorMode)
            {
                redSquare.GetComponentInChildren<SpriteRenderer>().color = Color.blue;
            }

            Rigidbody rb = redSquare.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomDirection = ClassicRandom.insideUnitSphere;
                randomDirection.y = Mathf.Abs(randomDirection.y);
                rb.AddForce(randomDirection * force, ForceMode.Impulse);
            }

            Destroy(redSquare, 0.3f);

            await Task.Yield();

        }
    }


    public static void SpawnBloodPuddle(Vector3 position, float sizeMultiplier = 1f, float rayDistance = 5f, bool spawnSingle = false)
    {
        if (bloodPuddlePrefab == null)
        {
            bloodPuddlePrefab = Resources.Load<GameObject>("Prefabs/Enemies/Gore/BloodPuddle");
        }

        void ApplyColor(GameObject obj, float projectorDepth)
        {
            Renderer rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                MaterialPropertyBlock mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb);
                rend.SetPropertyBlock(mpb);
            }
        }

        if (Physics.Raycast(position, Vector3.down, out RaycastHit groundHit, 10f))
        {
            GameObject bloodPuddle = Instantiate(
                bloodPuddlePrefab,
                new Vector3(groundHit.point.x, groundHit.point.y, groundHit.point.z),
                Quaternion.LookRotation(groundHit.normal)
            );

            bloodPuddle.transform.RotateAround(
                bloodPuddle.transform.position,
                groundHit.normal,
                Random.Range(0f, 360f)
            );

            if (sizeMultiplier != 1f)
                bloodPuddle.transform.localScale *= sizeMultiplier;

            ApplyColor(bloodPuddle, 3f);
        }
        else
        {
            GameObject bloodPuddle = Instantiate(
                bloodPuddlePrefab,
                position,
                bloodPuddlePrefab.transform.rotation
            );

            ApplyColor(bloodPuddle, 1f);
        }

        if (!spawnSingle)
        {
            float[] possibleAngles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

            List<float> anglesList = new List<float>(possibleAngles);
            for (int j = anglesList.Count - 1; j > 0; j--)
            {
                int k = Random.Range(0, j + 1);
                (anglesList[j], anglesList[k]) = (anglesList[k], anglesList[j]);
            }

            int numRays = Random.Range(3, anglesList.Count + 1);

            for (int i = 0; i < numRays; i++)
            {
                float angle = anglesList[i];
                Vector3 direction = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad),
                    0f,
                    Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                if (!Physics.Raycast(position, direction, out RaycastHit hit, rayDistance))
                    continue;

                if (!hit.collider.CompareTag("Environment"))
                    continue;

                if (Mathf.Abs(hit.normal.y) >= 0.2f)
                    continue;

                Vector3 splatterPos = hit.point + hit.normal * 0.045f;
                Quaternion splatterRot = Quaternion.LookRotation(hit.normal);
                GameObject splatterPuddle = Instantiate(bloodPuddlePrefab, splatterPos, splatterRot);

                float modAngle = Mathf.Abs(angle % 90f);
                float additionalRot;
                if (modAngle < 0.01f)
                    additionalRot = Mathf.Abs(angle % 180f) < 0.1f ? Random.Range(0, 2) * 180f : 90f;
                else
                    additionalRot = 45f;

                splatterPuddle.transform.RotateAround(splatterPos, hit.normal, additionalRot + 90f);

                ApplyColor(splatterPuddle, 1f);
            }
        }
    }



    public void KickEnemy(Transform kicker, int kickForce)
    {
        Vector3 kickDirection = (transform.position - kicker.position).normalized;
        rb.AddForce(kickDirection * kickForce, ForceMode.Impulse);
        //Counters.enemyKicked++;
    }
    public void KickEnemy(Vector3 kickerPos, int kickForce)
    {
        Vector3 kickDirection = (transform.position - kickerPos).normalized;
        rb.AddForce(kickDirection * kickForce, ForceMode.Impulse);
        //Counters.enemyKicked++;
    }

    public void KillEnemyOnTime(float time, DeathReason deathReason = default(DeathReason))
    {
        StartCoroutine(KillEnemyOnTimeCor(time, deathReason));
    }
    
    private IEnumerator KillEnemyOnTimeCor(float time, DeathReason deathReason)
    {
        yield return new WaitForSeconds(time);
        if (deathReason == default(DeathReason))
            deathReason = DeathReason.body;

        Die((byte)deathReason);
    }

    public void DrawRay(Vector3 start, Vector3 direction)
    {
        if (!rayObject || !rayLine)
        {
            rayObject = new GameObject("EnemyRay");
            rayObject.transform.transform.SetParent(transform);
            rayLine = rayObject.AddComponent<LineRenderer>();

            rayLine.positionCount = 2;

            rayLine.startWidth = 0.05f;
            rayLine.endWidth = 0.05f;
            rayLine.material = new Material(Shader.Find("Sprites/Default"));

            var color = Color.red;
            rayLine.startColor = color;
            color.a = 0f;
            rayLine.endColor = color;
        }

        rayLine.SetPosition(0, start);
        rayLine.SetPosition(1, start + direction);
    }


}
