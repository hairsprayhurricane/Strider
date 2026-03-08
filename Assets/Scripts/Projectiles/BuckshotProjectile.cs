using System.Collections;
using UnityEngine;

public class BuckshotProjectile : Projectile
{
    private Vector3 lastPosition;
    private Vector3 startPosition;
    private GameObject initSmokeBeam;
    private TrailRenderer trailRenderer;
    
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ModeId = Shader.PropertyToID("_Mode");
    
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

    void Update()
    {
        Vector3 currentPosition = transform.position + direction * speed * Time.deltaTime;
        
        if (Physics.Linecast(lastPosition, currentPosition, out RaycastHit hit, bulletLayerMask))
        {
            HandleHit(hit);
            return; 
        }

        ControlSmokeBeam(currentPosition);
        transform.position = currentPosition;
        lastPosition = currentPosition;
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

    private void ControlSmokeBeam(Vector3 endPosition)
    {
        if (trailRenderer == null)
        {
            InitializeTrailRenderer();
        }
        
        trailRenderer.AddPosition(endPosition);
    }

    private void InitializeTrailRenderer()
    {
        initSmokeBeam = new GameObject("SmokeTrail_MG");
        initSmokeBeam.transform.SetParent(transform);
        initSmokeBeam.transform.localPosition = Vector3.zero;
        
        trailRenderer = initSmokeBeam.AddComponent<TrailRenderer>();
        
        Material trailMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        trailMaterial.SetFloat(ModeId, 2);
        trailMaterial.SetInt(SrcBlendId, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        trailMaterial.SetInt(DstBlendId, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        trailMaterial.renderQueue = 3000;
        
        trailRenderer.material = trailMaterial;
        trailRenderer.startWidth = 0.1f;
        trailRenderer.endWidth = 0.05f;
        trailRenderer.time = 0.2f;
        trailRenderer.minVertexDistance = 0.05f;
        
        SetupTrailGradient(trailMaterial);
    }

    private void SetupTrailGradient(Material material)
    {
        Gradient gradient = new Gradient();
        
        GradientColorKey[] colorKeys = new GradientColorKey[3];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        
        colorKeys[0] = new GradientColorKey(new Color(1f, 1f, 1f), 0.0f);
        colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.8f, 0.8f), 0.5f);
        colorKeys[2] = new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1.0f);
        
        alphaKeys[0] = new GradientAlphaKey(0.0f, 0.0f);     
        alphaKeys[1] = new GradientAlphaKey(1.0f, 0.05f);    
        alphaKeys[2] = new GradientAlphaKey(1.0f, 1.0f);     
        
        gradient.SetKeys(colorKeys, alphaKeys);
        trailRenderer.colorGradient = gradient;
        
        material.color = new Color(1f, 1f, 1f, 0.3f);
        
        trailRenderer.enabled = false;
        trailRenderer.enabled = true;
    }

    void OnDestroy()
    {
        if (initSmokeBeam != null)
            Destroy(initSmokeBeam);
    }
}