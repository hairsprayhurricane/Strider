using System.Collections;
using UnityEngine;

public class Rocket : Projectile
{
    private Vector3 lastPosition;
    private Vector3 startPosition;
    
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ModeId = Shader.PropertyToID("_Mode");
    
    public Rocket(
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
            /*
            case "EnemyHead":
                var enemyHead = hit.collider.GetComponent<EnemyHead>();
                if (enemyHead != null) enemyHead.TakeDamage(175);
                Destroy(gameObject);
                break;

            case "Enemy":
                var enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null && enemy.TakeDamage(damage))
                    GunController.SpawnBloodDustAtHitPoint(hit.point);
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


            default:
                break;
            */
            case "Environment":
                Explosion.CreateExplosion(10, transform);
                Destroy(gameObject);
                break;
        }
    }
}