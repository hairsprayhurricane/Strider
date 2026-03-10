using System.Collections;
using UnityEngine;

public class BuckshotProjectile : Projectile
{
    public BuckshotProjectile(
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

        direction = transform.forward;

        Transform playerCamera = PlayerController.Instance.transform;
        Transform parentTransform = playerCamera.GetComponentInParent<Transform>();
        float rotationX = playerCamera.eulerAngles.x;
        float rotationY = parentTransform.eulerAngles.y;
        transform.GetChild(0).rotation = Quaternion.Euler(rotationX, rotationY, 0);

        lastPosition = transform.position;
        startPosition = transform.position;
    }

    public override void HandleHit(RaycastHit hit)
    {

        switch (hit.collider.tag)
        {
            case "Enemy":
                var enemy = hit.collider.GetComponent<Enemy>();
                enemy.TakeDamage(damage);
                Debug.Log(enemy.enemyHealth);
                Destroy(gameObject);
                break;
            case "Player":
                hit.collider.GetComponent<PlayerHealth>().DamagePlayer(damage);
                Destroy(gameObject);
                break;

            case "ExplosiveObject":
                var barrel = hit.collider.GetComponent<RedBarrel>();
                if (barrel != null)
                {
                    barrel.health -= damage;
                    if (barrel.health <= 0) barrel.Boom();
                }
                Destroy(gameObject);
                break;

            case "Environment":
                Destroy(gameObject);
                break;

            default:
                break;
        }
    }
}