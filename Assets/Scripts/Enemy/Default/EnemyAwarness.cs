using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyAwareness : MonoBehaviour
{
    public float awarenessRadius = 50f;
    protected float fieldOfView = 100f;
    protected float friendDetectField = 100;
    protected float autoAggroDistance;
    public bool isAggro;
    protected static Transform playersTransform;
    public List<EnemyAwareness> nearbyEnemies = new List<EnemyAwareness>();

    void Start()
    {
        if (playersTransform == null)
        {
            playersTransform = PlayerController.Instance.transform;
        }
        autoAggroDistance = awarenessRadius / 2;
        GetNearbyEnemies(friendDetectField);
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
            Vector3 toPlayer = playersTransform.position - transform.position;
            float sqrDist = toPlayer.sqrMagnitude;

            if (sqrDist < autoAggroDistance * autoAggroDistance)
            {
                isAggro = true;
                //if (PlayerController.isInvisible) isAggro = false;
            }
            else if (sqrDist < awarenessRadius * awarenessRadius)
            {
                float angle = Vector3.Angle(transform.forward, toPlayer);
                if (angle < fieldOfView / 2f)
                {
                    RaycastHit hit;
                    if (Physics.Raycast(transform.position, toPlayer.normalized, out hit, awarenessRadius))
                    {
                        if (hit.transform == playersTransform)
                        {
                            isAggro = true;
                        }
                    }
                }
            }

            if (isAggro)
            {
                TriggerNearbyEnemies();
            }

            yield return new WaitForSeconds(ClassicRandom.Range(0.3f, 0.6f));
        }
    }

    public void TriggerNearbyEnemies()
    {
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
}
