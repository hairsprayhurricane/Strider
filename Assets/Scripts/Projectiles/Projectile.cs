using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[Serializable]
public abstract class Projectile : MonoBehaviour
{
    public float timeBeforeDestroy = 0.1f;
    public static List<Projectile> bulletRegister = new List<Projectile>();
    public GameObject bulletHoleDecalPrefab;

    [HideInInspector] public Vector3 position;
    [HideInInspector] public Quaternion rotation;
    [HideInInspector] public Vector3 scale;

    public short damage;
    public float speed;
    public bool isShootedByPlayer = false;
    [HideInInspector] public Vector3 direction;

    public int prefabNumber;

    public LayerMask bulletLayerMask;

    public GameObject summoner;

    protected Vector3 lastPosition;
    protected Vector3 startPosition;
    protected GameObject initSmokeBeam;
    protected TrailRenderer trailRenderer;
    
    protected static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    protected static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    protected static readonly int ModeId = Shader.PropertyToID("_Mode");

    public Projectile(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        short damage,
        float speed,
        bool isShootedByPlayer,
        Vector3 direction
    )
    {
        this.position = position;
        this.rotation = rotation;
        this.scale = scale;
        this.damage = damage;
        this.speed = speed;
        this.isShootedByPlayer = isShootedByPlayer;
        this.direction = direction;
    }

    public static Projectile Spawn(GameObject prefab, Vector3 pos, Quaternion rot, GameObject owner, short damage = default)
    {
        var p = Instantiate(prefab, pos, rot).GetComponent<Projectile>();
        p.summoner = owner;
        if (damage != default) p.damage = damage;
        return p;
    }

    protected void Start()
    {
        bulletRegister.Add(this);
        Destroy(gameObject, timeBeforeDestroy);
        //prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

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

    public Projectile GetBulletSaveData()
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
        return this;
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

    public abstract void HandleHit(RaycastHit hit);


    protected void OnDestroy()
    {
        bulletRegister.Remove(this);

        if (initSmokeBeam != null)
            Destroy(initSmokeBeam);
    }

}

