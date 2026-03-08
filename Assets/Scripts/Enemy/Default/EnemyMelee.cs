using System.Collections;
using UnityEngine;

public class EnemyMelee : MonoBehaviour
{
    private float attackCooldown = 0.8f; 
    protected PlayerHealth player;
    public Enemy enemy;
    public EnemyGun enemyGun;
    public EnemyAi enemyAi;
    public EnemyAwareness enemyAwareness;
    private float elapsedTime = 0f;
    protected Animator animator;

    public short minDamage = 3;
    public short maxDamage = 10;


    void Start()
    {
        //enemy = GetComponentInParent<Enemy>();
        //enemyAi = enemy.enemyAi;
        //enemyAwareness = enemy.enemyAwareness;
        player = PlayerController.Instance.playerHealth;
        animator = GetComponentInParent<Animator>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && enemyAwareness.isAggro && enemyGun.CanShootAtPlayer(other))
        {
            AttackPlayer();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && enemyAwareness.isAggro && enemyAi.isMeleeAttacking)
        {
            StopAttacking();
        }
    }

    protected virtual void AttackPlayer()
    {
        enemyAi.isMeleeAttacking = true;
        animator.Play("enemyMelee1");
    }
    protected virtual void StopAttacking()
    {
        enemyAi.isMeleeAttacking = false;
        animator.Play("enemyMelee3");
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && enemyAi.isMeleeAttacking && enemyAwareness.isAggro)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= attackCooldown)
            {
                int damage = ClassicRandom.Range(minDamage, maxDamage);
                player.DamagePlayer((short)damage, transform);
                elapsedTime = 0f;
            }
            enemyAi.ResetPathForNav();
        }
    }
}