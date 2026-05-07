using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    public static readonly System.Collections.Generic.List<ThrowableProjectile> activeList = new();

    public PlayerItem item;
    public Vector3    initialVelocity;
    public Vector3    launchOrigin;
    public float      gravity = -25f;

    private const float MAX_LIFETIME            = 12f;
    private const float PROXIMITY_CHECK_INTERVAL = 0.3f;
    private const float PROXIMITY_RADIUS         = 3f;

    private Rigidbody _rb;
    private bool      _hasHit;
    private float     _proximityTimer;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        // useGravity and linearVelocity already set by ThrowableItemHandler.LaunchProjectile
        activeList.Add(this);
        Invoke(nameof(ForceDetonate), MAX_LIFETIME);
    }

    void ForceDetonate()
    {
        if (_hasHit) return;
        _hasHit = true;
        item.UseAtPoint(transform.position);
        Destroy(gameObject);
    }

    void FixedUpdate()
    {
        if (_hasHit) return;
        _rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);

        _proximityTimer += Time.fixedDeltaTime;
        if (_proximityTimer >= PROXIMITY_CHECK_INTERVAL)
        {
            _proximityTimer = 0f;
            EnemyAi.NotifyEnemiesNear(transform.position, launchOrigin, PROXIMITY_RADIUS);
        }
    }

    void OnDestroy()
    {
        activeList.Remove(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        _hasHit = true;
        item.UseAtPoint(collision.contacts[0].point);
        Destroy(gameObject);
    }
}
