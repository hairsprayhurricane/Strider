using System.Collections;
using UnityEngine;

public class Rocket : Projectile
{
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

        StartCoroutine(NonAvoidableExplosion());
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

        Explosion.CreateExplosion(10, transform);
        Destroy(gameObject);
    }
    
    public IEnumerator NonAvoidableExplosion()
    {
        yield return new WaitForSeconds(1);
        Explosion.CreateExplosion(10, transform);
        Destroy(gameObject);
    }
}