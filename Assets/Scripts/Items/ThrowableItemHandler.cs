using UnityEngine;

public class ThrowableItemHandler : MonoBehaviour
{
    public static ThrowableItemHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("ThrowableItemHandler");
                _instance = go.AddComponent<ThrowableItemHandler>();
            }
            return _instance;
        }
    }
    private static ThrowableItemHandler _instance;

    private PlayerItem _currentItem;
    private bool       _isAiming;
    private float      _holdTime;

    private const float TAP_THRESHOLD = 0.2f;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this); return; }
        _instance = this;
    }

    public void BeginAiming(PlayerItem item)
    {
        _currentItem                     = item;
        _isAiming                        = true;
        _holdTime                        = 0f;
        PlayerController.isPlayerMovable = false;
        PlayerController.Instance.hideRay = true;
    }

    public void CancelAiming()
    {
        if (!_isAiming) return;
        EndAiming();
    }

    // Called by CanvasSelectorController on GetKeyUp(F)
    public void OnKeyUp()
    {
        if (!_isAiming) return;

        if (_holdTime >= TAP_THRESHOLD)
            LaunchProjectile(GetAimWorldPosition());
        else
            _currentItem.UseAtPoint(PlayerController.Instance.transform.position);

        EndAiming();
    }

    void Update()
    {
        if (!_isAiming) return;

        _holdTime += Time.deltaTime;

        Vector3 start  = PlayerController.Instance.transform.position + Vector3.up * 1f;
        Vector3 target = GetAimWorldPosition();
        Vector3 vel    = ComputeLaunchVelocity(start, target);
        PlayerController.Instance.DrawThrowArc(start, vel, _currentItem.throwGravity);
    }

    // ----------------------------------------------------------------

    private void LaunchProjectile(Vector3 targetPos)
    {
        Vector3 start = PlayerController.Instance.transform.position + Vector3.up * 1f;
        Vector3 vel   = ComputeLaunchVelocity(start, targetPos);

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.position   = start;
        go.transform.localScale = Vector3.one * 0.3f;

        // Add Rigidbody manually BEFORE ThrowableProjectile so RequireComponent
        // finds it already configured — prevents first-frame gravity drop and
        // premature OnCollisionEnter underground.
        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity             = false;
        rb.linearVelocity         = vel;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;

        Physics.IgnoreCollision(
            go.GetComponent<Collider>(),
            PlayerController.Instance.GetComponent<Collider>());

        var proj             = go.AddComponent<ThrowableProjectile>();
        proj.item            = _currentItem;
        proj.gravity         = _currentItem.throwGravity;
        proj.initialVelocity = vel;
        proj.launchOrigin    = start;
    }

    private void EndAiming()
    {
        _isAiming                        = false;
        _currentItem                     = null;
        PlayerController.isPlayerMovable = true;
        PlayerController.Instance.RestoreRay();
    }

    private Vector3 GetAimWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
            return ray.GetPoint(dist);
        // fallback: forward from player
        return PlayerController.Instance.transform.position
             + PlayerController.Instance.transform.forward * 10f;
    }

    // Computes initial velocity so the projectile lands at target.
    // Horizontal speed = throwSpeed; vertical derived from ballistics.
    private Vector3 ComputeLaunchVelocity(Vector3 start, Vector3 target)
    {
        float   speed      = _currentItem.throwSpeed;
        float   g          = _currentItem.throwGravity; // negative

        Vector3 toTarget   = target - start;
        Vector3 horizontal = new(toTarget.x, 0f, toTarget.z);
        float   hDist      = horizontal.magnitude;
        float   vDist      = toTarget.y;

        if (hDist < 0.01f) return Vector3.up * speed;

        float t  = hDist / speed;
        float vy = (vDist - 0.5f * g * t * t) / t;

        return horizontal.normalized * speed + Vector3.up * vy;
    }
}
