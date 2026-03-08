using System;
using System.Collections.Generic;
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

    protected void Start()
    {
        bulletRegister.Add(this);
        Destroy(gameObject, timeBeforeDestroy);
        //prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);

    }

    void OnDestroy()
    {
        bulletRegister.Remove(this);
    }

    public Projectile GetBulletSaveData()
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
        return this;
    }

    public abstract void HandleHit(RaycastHit hit);

}

