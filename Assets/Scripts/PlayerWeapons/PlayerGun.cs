using Unity.VisualScripting;
using UnityEngine;


public enum GunType
    {
        // Fists, boring
        //Chainsaw, boring
        Pistol,
        Shotgun,
        MachineGun,
        RocketLauncher,
        PlasmaGun,
        BFG,
        Unmaker
    }
    public enum GunClass
    {
        Light,
        Medium,
        Heavy
    }
public abstract class PlayerGun : MonoBehaviour
{
    protected Transform playerPos;
    public GunType weaponType;
    public GunClass weaponClass;
    public AmmoType weaponAmmo;
    public bool isAutomatic = false;
    public bool isSilenced = false;
    public bool isActive;
    [Header("Shooting")]
    public short damage = 30;
    public float range = 100f;
    public float fireRate = 0.01f;
    protected int bulletSpeed = 100;
    protected int bulletPerShot = 1;
    public int bulletSpread = 1;
    protected float nextTimeToFire;
    public GameObject mainProjectilePrefab;
    [Header("Ammunition")]
    public bool isInfiniteAmmo;
    [Header("Sounds")]
    public AudioClip shotSound;
    public AudioClip noAmmoSound;
    public AudioClip weaponChangeSound;
    public AudioSource audioSource;

    [Header("Recoil")]
    public float shakeForceMultiplier = 1f;

    void Start()
    {
        playerPos = PlayerController.Instance.transform;
    }

    public virtual void Update()
    {
        if (!isActive) return;

        if ((isAutomatic ? Input.GetKey(KeyCode.Mouse0) : Input.GetKeyDown(KeyCode.Mouse0)) && Time.time > nextTimeToFire)
        {
            if (isInfiniteAmmo || GunController.Instance.GetAmmo(weaponAmmo) >= bulletPerShot)
            {
                Fire();
            }
            else
            {
                audioSource.PlayOneShot(noAmmoSound);
            }
        }
    }

    public abstract void Fire();

    public void AfterFire()
    {
        nextTimeToFire = Time.time + fireRate;

        if (!isSilenced) EnemyManager.AggroAll();

        audioSource.PlayOneShot(shotSound);

        if (!isInfiniteAmmo)
        {
            GunController.Instance.TryConsumeAmmo(weaponAmmo, bulletPerShot);
        }

        SimulateRecoil(shakeForceMultiplier);
    }

    protected void SimulateRecoil(float shakeForceMultiplier = 1)
    {
        PlayerController.Instance.ShakeCamera(0.3f, 0.5f * shakeForceMultiplier);
    }

    /*protected virtual void SimulateRecoil(float recoilPitch)
    {
        PlayerController playerController = PlayerController.Instance;
        if (playerController != null)
        {
            float yawRecoil = Random.Range(-recoilYawMax, recoilYawMax);
            playerController.ApplyRecoil(recoilPitch, yawRecoil, recoilDuration);
        }
    }*/


    public void UpdateAmmo()
    {
        //canvasManager.UpdateAmmo(ammo, this);
    }

    public GunType GetGunType()
    {
        return weaponType;
    }

}
