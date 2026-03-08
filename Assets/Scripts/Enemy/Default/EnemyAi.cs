using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAi : MonoBehaviour
{
    public AudioSource playerSpottedSound;
    public bool isStatic = false;

    [HideInInspector] public EnemyAwareness enemyAwareness;
    protected Transform playersTransform;
    [HideInInspector] public NavMeshAgent enemyNavMeshAgent;
    protected EnemyGun enemyGun;
    protected Enemy enemy;

    public bool isPlayerSpotted;
    public bool isInSight;

    public bool isMovingToPlayer;
    public bool isMeleeAttacking;

    protected float enemySpeed;
    protected float maxSpeed = 50;
    protected float minSpeed = 15;
    protected float baseEnemySpeed;
    protected float aggroInterval = 0.5f;
    protected float visibilityCheckInterval = 0.5f;
    protected float pathUpdateInterval = 0.2f;
    protected float randomPointRadius = 10f;

    private float preferredDistance = 15f;
    private const float preferredDistanceMin = 5f;
    private const float preferredDistanceMax = 25f;
    private const float distanceTolerance = 10f;

    private bool isWandering = false;
    private Coroutine wanderCoroutine;

    private Coroutine stopWalkCoroutine;

    private LayerMask environmentMask;

    public void Start()
    {
        enemyAwareness = GetComponent<EnemyAwareness>();
        playersTransform = PlayerController.Instance.transform;
        enemyNavMeshAgent = GetComponent<NavMeshAgent>();
        enemy = GetComponent<Enemy>();
        enemyGun = GetComponentInChildren<EnemyGun>();

        environmentMask = LayerMask.GetMask("Environment", "Cover");

        if (enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.enabled = true;
            enemyNavMeshAgent.isStopped = isStatic;
            enemyNavMeshAgent.autoBraking = false;
            enemyNavMeshAgent.stoppingDistance = preferredDistanceMin;
            enemyNavMeshAgent.updateRotation = true;
        }

        isInSight = false;
        isPlayerSpotted = false;
        isMovingToPlayer = false;
        isMeleeAttacking = false;

        enemySpeed = 15f;
        aggroInterval = 0.4f;
        visibilityCheckInterval = 0.4f;
        pathUpdateInterval = 0.8f;
        randomPointRadius = 10f;
        baseEnemySpeed = enemySpeed;
        if (enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.speed = baseEnemySpeed;
        }

    }

    private void LockPreferredDistance()
    {
        if (playersTransform == null) return;
        float currentDist = Vector3.Distance(transform.position, playersTransform.position);
        preferredDistance = Mathf.Clamp(currentDist, preferredDistanceMin, preferredDistanceMax);
    }

    private void Update()
    {
        if (enemyAwareness && enemyAwareness.isAggro)
        {
            if (!isPlayerSpotted)
            {
                isPlayerSpotted = true;
                LockPreferredDistance();
                if (playerSpottedSound != null) playerSpottedSound.Play();
            }

            if (!IsInvoking("AggroBehavior"))
            {
                InvokeRepeating("AggroBehavior", 0f, aggroInterval);
            }
        }
        else
        {
            if (IsInvoking("AggroBehavior"))
            {
                CancelInvoke("AggroBehavior");
                StopWandering();

                if (enemyNavMeshAgent != null && !isStatic)
                {
                    enemyNavMeshAgent.ResetPath();
                    enemyNavMeshAgent.isStopped = false;
                }

                isPlayerSpotted = false;
            }
        }
    }

    private void AggroBehavior()
    {
        if (isMeleeAttacking) return;

        if (isStatic && enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.isStopped = true;
            LookAtPlayer();
            return;
        }

        EnforceMinimumDistance();

        if (CheckIsInSight())
        {
            LookAtPlayer();
            isMovingToPlayer = false;

            if (!isWandering)
            {
                wanderCoroutine = StartCoroutine(WanderAtPreferredDistance());
            }
        }
        else
        {
            SeekPlayer();
        }
    }

    private void EnforceMinimumDistance()
    {
        if (playersTransform == null || enemyNavMeshAgent == null || isStatic) return;

        float dist = Vector3.Distance(transform.position, playersTransform.position);
        if (dist < preferredDistance)
        {
            StopWandering();
            isMovingToPlayer = false;

            Vector3 dirAway = (transform.position - playersTransform.position).normalized;
            Vector3 retreatPoint = transform.position + dirAway * (preferredDistance - dist + 2f);

            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatPoint, out hit, randomPointRadius * 2f, NavMesh.AllAreas))
            {
                enemyNavMeshAgent.SetDestination(hit.position);
            }
        }
    }

    private IEnumerator WanderAtPreferredDistance()
    {
        isWandering = true;

        while (true)
        {
            if (enemyNavMeshAgent == null || isStatic) break;

            if (IsAtPreferredDistance())
            {
                Vector3 randomDest = GetRandomDirection();
                enemyNavMeshAgent.SetDestination(randomDest);

                yield return new WaitUntil(() =>
                    enemyNavMeshAgent == null ||
                    (!enemyNavMeshAgent.pathPending &&
                     enemyNavMeshAgent.remainingDistance <= enemyNavMeshAgent.stoppingDistance + 0.5f));
            }
            else
            {
                Vector3 returnDest = GetPositionAtPreferredDistance();
                enemyNavMeshAgent.SetDestination(returnDest);

                yield return new WaitUntil(() =>
                    enemyNavMeshAgent == null ||
                    (!enemyNavMeshAgent.pathPending &&
                     enemyNavMeshAgent.remainingDistance <= enemyNavMeshAgent.stoppingDistance + 0.5f));
            }

            yield return new WaitForSeconds(ClassicRandom.Range(0.2f, 0.8f));
        }

        isWandering = false;
    }

    private void StopWandering()
    {
        if (wanderCoroutine != null)
        {
            StopCoroutine(wanderCoroutine);
            wanderCoroutine = null;
        }
        isWandering = false;
    }

    public void ResetPathForNav()
    {
        if (enemyNavMeshAgent != null && !isStatic)
        {
            enemyNavMeshAgent.ResetPath();
            enemyNavMeshAgent.isStopped = false;
        }
    }

    private void SeekPlayer()
    {
        StopWandering();

        if (!isStatic && enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.speed = baseEnemySpeed;
            enemyNavMeshAgent.isStopped = false;
        }

        isMovingToPlayer = true;

        if (!isStatic)
        {
            StartCoroutine(MoveToPlayer());
            StartCoroutine(CheckPlayerVisibility());
        }
    }

    private IEnumerator CheckPlayerVisibility()
    {
        while (isMovingToPlayer)
        {
            yield return new WaitForSeconds(visibilityCheckInterval);

            if (CheckIsInSight())
            {
                isMovingToPlayer = false;
                yield break;
            }
        }
    }

    private IEnumerator MoveToPlayer()
    {
        while (isMovingToPlayer)
        {
            if (enemyNavMeshAgent == null || playersTransform == null || isStatic) yield break;

            float distanceToPlayer = Vector3.Distance(transform.position, playersTransform.position);

            if (distanceToPlayer <= preferredDistance)
            {
                enemyNavMeshAgent.ResetPath();
                yield return new WaitForSeconds(pathUpdateInterval);
                continue;
            }

            NavMeshPath path = new NavMeshPath();
            bool hasPath = enemyNavMeshAgent.CalculatePath(playersTransform.position, path);

            if (hasPath && path.status == NavMeshPathStatus.PathComplete)
            {
                enemyNavMeshAgent.SetDestination(playersTransform.position);
                enemyNavMeshAgent.isStopped = false;
                enemyNavMeshAgent.speed = distanceToPlayer >= 50f ? maxSpeed : minSpeed;
            }
            else
            {
                enemyNavMeshAgent.ResetPath();
                Vector3 fallbackPoint = playersTransform.position + ClassicRandom.insideUnitSphere * 5f;
                enemyNavMeshAgent.SetDestination(fallbackPoint);
            }

            LookAtPlayer();
            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

    private Vector3 GetPositionAtPreferredDistance()
    {
        if (playersTransform == null) return transform.position;

        Vector3 dirToEnemy = (transform.position - playersTransform.position).normalized;
        Vector3 basePoint = playersTransform.position + dirToEnemy * preferredDistance;

        Vector3 randomOffset = ClassicRandom.insideUnitSphere * randomPointRadius;
        randomOffset.y = 0f;
        Vector3 candidatePoint = basePoint + randomOffset;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(candidatePoint, out hit, randomPointRadius * 2f, NavMesh.AllAreas))
            return hit.position;

        if (NavMesh.SamplePosition(basePoint, out hit, randomPointRadius * 2f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private bool IsAtPreferredDistance()
    {
        if (playersTransform == null) return true;
        float dist = Vector3.Distance(transform.position, playersTransform.position);
        return Mathf.Abs(dist - preferredDistance) <= distanceTolerance;
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 randomPoint = ClassicRandom.insideUnitSphere * randomPointRadius + transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, randomPointRadius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }

    private void LookAtPlayer()
    {
        if (playersTransform == null) return;

        Vector3 dir = playersTransform.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
    }

    private bool CheckIsInSight()
    {
        if (playersTransform == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 directionToPlayer = playersTransform.position - origin;

        if (Physics.Raycast(origin, directionToPlayer.normalized, out RaycastHit hit, Mathf.Infinity, environmentMask))
        {
            if (hit.distance < directionToPlayer.magnitude)
            {
                isInSight = false;
                return false;
            }
        }
        if (enemyGun != null)
        {
            Collider playerCollider = playersTransform.GetComponent<Collider>();
            if (playerCollider != null && enemyGun.IsPlayerInShootingRange(playerCollider))
            {
                isInSight = true;
                return true;
            }
        }

        isInSight = false;
        return false;
    }

    public void StopWalk(float time)
    {
        if (stopWalkCoroutine != null)
            StopCoroutine(stopWalkCoroutine);

        stopWalkCoroutine = StartCoroutine(StopWalkCoroutine(time));
    }

    private IEnumerator StopWalkCoroutine(float duration)
    {
        if (enemyNavMeshAgent != null && !isStatic)
        {
            enemyNavMeshAgent.isStopped = true;
            enemyNavMeshAgent.speed = 0f;
        }

        isMovingToPlayer = false;
        isMeleeAttacking = false;

        StopAllCoroutines();

        yield return new WaitForSeconds(duration);

        if (enemyNavMeshAgent != null && !isStatic)
        {
            enemyNavMeshAgent.isStopped = false;
            enemyNavMeshAgent.speed = baseEnemySpeed;
        }

        stopWalkCoroutine = null;
    }

    public void ResumeWalk()
    {
        if (stopWalkCoroutine != null)
        {
            StopCoroutine(stopWalkCoroutine);
            stopWalkCoroutine = null;

            if (enemyNavMeshAgent != null && !isStatic)
            {
                enemyNavMeshAgent.isStopped = false;
                enemyNavMeshAgent.speed = baseEnemySpeed;
            }

            if (enemyAwareness && enemyAwareness.isAggro && !isStatic)
            {
                isMovingToPlayer = true;
                SeekPlayer();
            }
        }
    }
}