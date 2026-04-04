using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAi : MonoBehaviour
{
    public AudioSource playerSpottedSound;
    public bool isStatic = false;
    public bool seekAtStart = true;

    [HideInInspector] public EnemyAwareness enemyAwareness;
    [HideInInspector] public NavMeshAgent enemyNavMeshAgent;
    protected Transform playersTransform;
    protected EnemyGun enemyGun;
    protected Enemy enemy;

    public bool isPlayerSpotted;
    public bool isInSight;
    public bool isMovingToPlayer;
    public bool isMeleeAttacking;

    protected float baseEnemySpeed;
    protected float maxSpeed = 50f;
    protected float minSpeed = 15f;
    protected float aggroInterval = 0.4f;
    protected float pathUpdateInterval = 0.8f;
    protected float randomPointRadius = 10f;

    private float preferredDistance = 15f;
    private const float preferredDistanceMin = 5f;
    private const float preferredDistanceMax = 25f;
    private const float distanceTolerance = 10f;
    private const float calmSpeed = 3.5f;

    private bool isWandering;
    private bool isPatrolling;

    private Coroutine wanderCoroutine;
    private Coroutine chaseCoroutine;
    private Coroutine stopWalkCoroutine;
    private Coroutine patrolCoroutine;
    private Coroutine investigateCoroutine;

    private float patrolRadius = 20f;
    private List<Vector3> patrolPoints = new List<Vector3>();
    private int currentPatrolIndex;

    private LayerMask environmentMask;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void OnEnable()  => EnemyFSM.OnStateChanged += OnGlobalStateChanged;
    private void OnDisable() => EnemyFSM.OnStateChanged -= OnGlobalStateChanged;

    public void Start()
    {
        enemyAwareness    = GetComponent<EnemyAwareness>();
        enemyNavMeshAgent = GetComponent<NavMeshAgent>();
        enemy             = GetComponent<Enemy>();
        enemyGun          = GetComponentInChildren<EnemyGun>();
        playersTransform  = PlayerController.Instance.transform;

        environmentMask = LayerMask.GetMask("Environment", "Cover");
        baseEnemySpeed  = 15f;

        if (enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.enabled         = true;
            enemyNavMeshAgent.isStopped       = isStatic;
            enemyNavMeshAgent.autoBraking     = false;
            enemyNavMeshAgent.stoppingDistance = preferredDistanceMin;
            enemyNavMeshAgent.updateRotation  = true;
            enemyNavMeshAgent.speed           = baseEnemySpeed;
        }

        GeneratePatrolPoints();

        if (seekAtStart && !isStatic)
            StartPatrol();
    }

    private void Update()
    {
        if (enemyAwareness && enemyAwareness.isAggro)
        {
            if (!isPlayerSpotted)
            {
                isPlayerSpotted = true;
                LockPreferredDistance();
                StopPatrol();
                StopInvestigate();
                if (playerSpottedSound != null) playerSpottedSound.Play();
            }

            if (!IsInvoking("AggroBehavior"))
                InvokeRepeating("AggroBehavior", 0f, aggroInterval);
        }
        else
        {
            if (IsInvoking("AggroBehavior"))
            {
                CancelInvoke("AggroBehavior");
                StopWandering();
                StopChasing();

                if (enemyNavMeshAgent != null && !isStatic)
                {
                    enemyNavMeshAgent.ResetPath();
                    enemyNavMeshAgent.isStopped = false;
                }

                isPlayerSpotted = false;
                isMovingToPlayer = false;

                if (seekAtStart && !isStatic)
                    StartPatrol();
            }
        }
    }

    // ── FSM ──────────────────────────────────────────────────────────────────

    private void OnGlobalStateChanged(EnemyFSM.GlobalState newState)
    {
        if (enemyAwareness != null && enemyAwareness.isAggro) return;

        if (newState == EnemyFSM.GlobalState.Search)
        {
            StopPatrol();
            StartPatrol(baseEnemySpeed);
        }
        else if (newState == EnemyFSM.GlobalState.Calm)
        {
            if (seekAtStart && !isStatic)
                StartPatrol();
        }
    }

    // ── Aggro ─────────────────────────────────────────────────────────────────

    private void AggroBehavior()
    {
        if (isMeleeAttacking) return;

        if (isStatic && enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.isStopped = true;
            LookAtPlayer();
            return;
        }

        if (CheckIsInSight())
        {
            StopChasing();
            isMovingToPlayer = false;

            LookAtPlayer();
            EnforceMinimumDistance();

            if (!isWandering)
                wanderCoroutine = StartCoroutine(WanderAtPreferredDistance());
        }
        else
        {
            StopWandering();

            if (chaseCoroutine == null)
            {
                isMovingToPlayer = true;
                chaseCoroutine = StartCoroutine(ChasePlayer());
            }
        }
    }

    private IEnumerator ChasePlayer()
    {
        if (enemyNavMeshAgent != null && !isStatic)
        {
            enemyNavMeshAgent.isStopped = false;
            enemyNavMeshAgent.speed = baseEnemySpeed;
        }

        while (isMovingToPlayer)
        {
            if (enemyNavMeshAgent == null || playersTransform == null || isStatic) break;

            if (CheckIsInSight())
            {
                isMovingToPlayer = false;
                break;
            }

            float dist = Vector3.Distance(transform.position, playersTransform.position);
            enemyNavMeshAgent.speed = dist >= 50f ? maxSpeed : minSpeed;
            enemyNavMeshAgent.isStopped = false;
            enemyNavMeshAgent.SetDestination(playersTransform.position);

            yield return new WaitForSeconds(pathUpdateInterval);
        }

        chaseCoroutine = null;
    }

    private void StopChasing()
    {
        if (chaseCoroutine != null)
        {
            StopCoroutine(chaseCoroutine);
            chaseCoroutine = null;
        }
        isMovingToPlayer = false;
    }

    private IEnumerator WanderAtPreferredDistance()
    {
        isWandering = true;

        while (true)
        {
            if (enemyNavMeshAgent == null || isStatic) break;

            Vector3 dest = IsAtPreferredDistance()
                ? GetRandomDirection()
                : GetPositionAtPreferredDistance();

            enemyNavMeshAgent.SetDestination(dest);

            yield return new WaitUntil(() =>
                enemyNavMeshAgent == null ||
                (!enemyNavMeshAgent.pathPending &&
                 enemyNavMeshAgent.remainingDistance <= enemyNavMeshAgent.stoppingDistance + 0.5f));

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

    // ── Patrol ────────────────────────────────────────────────────────────────

    private void GeneratePatrolPoints()
    {
        patrolPoints.Clear();
        Vector3 origin = transform.position;
        int attempts = 0;

        while (patrolPoints.Count < 5 && attempts < 30)
        {
            attempts++;
            Vector3 candidate = origin + new Vector3(
                ClassicRandom.Range(-patrolRadius, patrolRadius),
                0f,
                ClassicRandom.Range(-patrolRadius, patrolRadius));

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, patrolRadius * 0.5f, NavMesh.AllAreas))
                patrolPoints.Add(hit.position);
        }
    }

    private void StartPatrol(float speed = calmSpeed)
    {
        if (patrolCoroutine != null) return;
        patrolCoroutine = StartCoroutine(PatrolBehavior(speed));
    }

    private void StopPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
        isPatrolling = false;
    }

    private IEnumerator PatrolBehavior(float speed)
    {
        isPatrolling = true;

        if (enemyNavMeshAgent != null)
        {
            enemyNavMeshAgent.isStopped = false;
            enemyNavMeshAgent.speed = speed;
        }

        while (isPatrolling)
        {
            if (enemyNavMeshAgent == null || isStatic || patrolPoints.Count == 0) break;

            enemyNavMeshAgent.SetDestination(patrolPoints[currentPatrolIndex]);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;

            yield return new WaitUntil(() =>
                enemyNavMeshAgent == null ||
                (!enemyNavMeshAgent.pathPending &&
                 enemyNavMeshAgent.remainingDistance <= enemyNavMeshAgent.stoppingDistance + 0.5f));

            yield return new WaitForSeconds(ClassicRandom.Range(0.5f, 2f));
        }

        isPatrolling = false;
        patrolCoroutine = null;
    }

    // ── Positioning helpers ───────────────────────────────────────────────────

    private void LockPreferredDistance()
    {
        if (playersTransform == null) return;
        float dist = Vector3.Distance(transform.position, playersTransform.position);
        preferredDistance = Mathf.Clamp(dist, preferredDistanceMin, preferredDistanceMax);
    }

    private void EnforceMinimumDistance()
    {
        if (playersTransform == null || enemyNavMeshAgent == null || isStatic) return;

        float dist = Vector3.Distance(transform.position, playersTransform.position);
        if (dist >= preferredDistance) return;

        StopWandering();
        Vector3 dirAway = (transform.position - playersTransform.position).normalized;
        Vector3 retreatPoint = transform.position + dirAway * (preferredDistance - dist + 2f);

        if (NavMesh.SamplePosition(retreatPoint, out NavMeshHit hit, randomPointRadius * 2f, NavMesh.AllAreas))
            enemyNavMeshAgent.SetDestination(hit.position);
    }

    private bool IsAtPreferredDistance()
    {
        if (playersTransform == null) return true;
        float dist = Vector3.Distance(transform.position, playersTransform.position);
        return Mathf.Abs(dist - preferredDistance) <= distanceTolerance;
    }

    private Vector3 GetPositionAtPreferredDistance()
    {
        if (playersTransform == null) return transform.position;

        Vector3 dir = (transform.position - playersTransform.position).normalized;
        Vector3 basePoint = playersTransform.position + dir * preferredDistance;
        Vector3 offset = ClassicRandom.insideUnitSphere * randomPointRadius;
        offset.y = 0f;

        if (NavMesh.SamplePosition(basePoint + offset, out NavMeshHit hit, randomPointRadius * 2f, NavMesh.AllAreas))
            return hit.position;
        if (NavMesh.SamplePosition(basePoint, out hit, randomPointRadius * 2f, NavMesh.AllAreas))
            return hit.position;

        return transform.position;
    }

    private Vector3 GetRandomDirection()
    {
        Vector3 candidate = ClassicRandom.insideUnitSphere * randomPointRadius + transform.position;
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, randomPointRadius, NavMesh.AllAreas))
            return hit.position;
        return transform.position;
    }

    private void LookAtPlayer()
    {
        if (playersTransform == null) return;
        Vector3 dir = playersTransform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
    }

    private bool CheckIsInSight()
    {
        if (playersTransform == null) return false;

        Vector3 origin = transform.position + Vector3.up;
        Vector3 toPlayer = playersTransform.position - origin;

        if (Physics.Raycast(origin, toPlayer.normalized, out RaycastHit hit, Mathf.Infinity, environmentMask))
        {
            if (hit.distance < toPlayer.magnitude)
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

    // ── Public API ────────────────────────────────────────────────────────────

    public void InvestigatePoint(Vector3 point)
    {
        if (investigateCoroutine != null) StopCoroutine(investigateCoroutine);
        StopPatrol();
        investigateCoroutine = StartCoroutine(InvestigateCoroutine(point));
    }

    private void StopInvestigate()
    {
        if (investigateCoroutine == null) return;
        StopCoroutine(investigateCoroutine);
        investigateCoroutine = null;
    }

    private IEnumerator InvestigateCoroutine(Vector3 point)
    {
        if (enemyNavMeshAgent == null || isStatic) yield break;

        const float stopShortDistance = 3f;
        Vector3 dir = (point - transform.position).normalized;
        Vector3 destination = point - dir * stopShortDistance;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            destination = hit.position;

        enemyNavMeshAgent.speed = baseEnemySpeed;
        enemyNavMeshAgent.isStopped = false;
        enemyNavMeshAgent.SetDestination(destination);

        yield return new WaitUntil(() =>
            enemyNavMeshAgent == null ||
            (enemyAwareness != null && enemyAwareness.isAggro) ||
            (!enemyNavMeshAgent.pathPending &&
             enemyNavMeshAgent.remainingDistance <= enemyNavMeshAgent.stoppingDistance + 0.5f));

        investigateCoroutine = null;

        if (enemyAwareness != null && enemyAwareness.isAggro) yield break;

        if (seekAtStart && !isStatic)
            StartPatrol();
    }

    public void ResetPathForNav()
    {
        if (enemyNavMeshAgent != null && !isStatic)
        {
            enemyNavMeshAgent.ResetPath();
            enemyNavMeshAgent.isStopped = false;
        }
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

        StopWandering();
        StopChasing();

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
        if (stopWalkCoroutine == null) return;

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
            chaseCoroutine = StartCoroutine(ChasePlayer());
        }
    }
}
