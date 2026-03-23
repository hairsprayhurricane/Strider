using System.Collections;
using UnityEngine;

public class Flame : Projectile
{
    public GameObject particle;
    public Vector3 currentScale;
    public Vector3 defaultScale;
    public Flame(
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

    public override void Start()
    {
        //base.Start();

        transform.localScale *= ClassicRandom.value;

        direction = transform.forward;

        Transform playerCamera = PlayerController.Instance.transform;
        Transform parentTransform = playerCamera.GetComponentInParent<Transform>();
        float rotationX = playerCamera.eulerAngles.x;
        float rotationY = parentTransform.eulerAngles.y;
        transform.GetChild(0).rotation = Quaternion.Euler(rotationX, rotationY, 0);

        lastPosition = transform.position;
        startPosition = transform.position;

        Destroy(gameObject, timeBeforeDestroy+ClassicRandom.value);



        defaultScale = gameObject.transform.localScale;
        currentScale = defaultScale / 2.5f;
        gameObject.transform.localScale = currentScale;
        SpawnParticles();
        StartCoroutine(ParticleCoroutine());
        StartCoroutine(ScaleCoroutine());

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
                Destroy(gameObject);
                break;
            
            case "Player":
                hit.collider.GetComponent<PlayerHealth>().DamagePlayer(damage);
                break;

            case "ExplosiveObject":
                var barrel = hit.collider.GetComponent<RedBarrel>();
                if (barrel != null)
                {
                    barrel.health -= damage;
                    if (barrel.health <= 0) barrel.Boom();
                }
                break;

            case "Environment":
                break;

            default:
                break;
        }
    }

    private IEnumerator DamageEnemyCor(Enemy enemy)
    {
        while(!enemy.isDead)
        {
            yield return new WaitForSeconds(ClassicRandom.value*3);
            enemy.TakeDamage(ClassicRandom.RangeShort(5,10));
        }

        //Destroy(gameObject);
    }

    // -- legacy

    async void SpawnParticles()
    {
        for (int i = 0; i < 3; i++)
        {
            SpriteRenderer sr = particle.GetComponent<SpriteRenderer>();

            GameObject spawnedParticle = Instantiate(particle, new Vector3(
                transform.position.x + ClassicRandom.Range(-2, 2), 
                transform.position.y + ClassicRandom.Range(-2, 2), 
                transform.position.z + ClassicRandom.Range(-2, 2)), 
                Quaternion.identity);
            
            spawnedParticle.GetComponent<SpriteRenderer>().color = new Color(1, Random.Range(0f, 1f), 0);

            Vector3 kickDirection = -Camera.main.transform.forward;
            spawnedParticle.GetComponent<Rigidbody>().AddForce(kickDirection * 5, ForceMode.Impulse);

            Destroy(spawnedParticle, ClassicRandom.value);
        }
    }

    IEnumerator ParticleCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.4f);
            SpawnParticles();
        }
    }
    IEnumerator ScaleCoroutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            gameObject.transform.localScale = Vector3.Lerp(currentScale, defaultScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        gameObject.transform.localScale = defaultScale;
    }
    
}