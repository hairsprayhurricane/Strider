using System.Collections;
using UnityEngine;

public class FireProjectile : Projectile
{
    public FireProjectile(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        short damage,
        float speed,
        bool isShootedByPlayer,
        Vector3 direction
    ) : base(position, rotation, scale, damage, speed, isShootedByPlayer, direction)
    {
    }

    void Start()
    {
        base.Start();

        transform.localScale *= ClassicRandom.value;

        direction = transform.forward;

        Transform playerCamera = PlayerController.Instance.transform;
        Transform parentTransform = playerCamera.GetComponentInParent<Transform>();
        float rotationX = playerCamera.eulerAngles.x;
        float rotationY = parentTransform.eulerAngles.y;
        transform.GetChild(0).rotation = Quaternion.Euler(rotationX, rotationY, 0);

        lastPosition = transform.position;
        startPosition = transform.position;

    }

    void Update()
    {
        Vector3 currentPosition = transform.position + direction * speed * Time.deltaTime;
        
        if (Physics.Linecast(lastPosition, currentPosition, out RaycastHit hit, bulletLayerMask))
        {
            HandleHit(hit);
            return; 
        }

        transform.position = currentPosition;
        lastPosition = currentPosition;
    }

    public override void HandleHit(RaycastHit hit)
    {
        switch (hit.collider.tag)
        {
            case "Enemy":
                var enemy = hit.collider.GetComponent<Enemy>();
                if (isStealthShot && !enemy.enemyAwareness.isAggro) enemy.Die();
                else enemy.TakeDamage(damage);
                enemy.StartCoroutine(DamageEnemyCor(enemy));
                //Destroy(gameObject);
                break;
            
            case "Player":
                hit.collider.GetComponent<PlayerHealth>().DamagePlayer(damage);
                //Destroy(gameObject);
                break;

            case "ExplosiveObject":
                var barrel = hit.collider.GetComponent<RedBarrel>();
                if (barrel != null)
                {
                    barrel.health -= damage;
                    if (barrel.health <= 0) barrel.Boom();
                }
                //Destroy(gameObject);
                break;

            case "Environment":
                //Destroy(gameObject);
                break;

            default:
                break;
        }
    }

    private IEnumerator DamageEnemyCor(Enemy enemy)
    {
        while(!enemy.isDead)
        {
            yield return new WaitForSeconds(ClassicRandom.value);
            enemy.TakeDamage((short)(damage/2));
        }
    }
    
}