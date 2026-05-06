using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    public PlayerItem item;
    public Vector3    initialVelocity;
    public float      gravity = -25f;

    private Rigidbody _rb;
    private bool      _hasHit;

    void Start()
    {
        _rb            = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.linearVelocity = initialVelocity;
    }

    void FixedUpdate()
    {
        if (_hasHit) return;
        _rb.linearVelocity += Vector3.up * gravity * Time.fixedDeltaTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        _hasHit = true;
        item.UseAtPoint(collision.contacts[0].point);
        Destroy(gameObject);
    }
}
