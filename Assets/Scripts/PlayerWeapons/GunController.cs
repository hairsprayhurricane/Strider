using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public const int MAX_WEAPONS = 10;

    private Dictionary<int, PlayerGun> playerGuns = new Dictionary<int, PlayerGun>();

    [SerializeField] private AmmunitionData ammunitionData;

    private int currentGunIndex = -1;

    public Transform weaponHolder;

    private static GunController _instance;
    public static GunController Instance { get { return _instance; } }

    public void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;

        if (ammunitionData == null)
            ammunitionData = new AmmunitionData();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchWeapon(4);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchWeapon(5);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SwitchWeapon(6);
        if (Input.GetKeyDown(KeyCode.Alpha7)) SwitchWeapon(7);
        if (Input.GetKeyDown(KeyCode.Alpha8)) SwitchWeapon(8);
        if (Input.GetKeyDown(KeyCode.Alpha9)) SwitchWeapon(9);
        if (Input.GetKeyDown(KeyCode.Alpha0)) SwitchWeapon(0);
    }

    public void AddWeapon(PlayerGun gunPrefab, byte index)
    {
        if (playerGuns.Count >= MAX_WEAPONS)
        {
            Debug.Log("Weapon slots full");
            return;
        }

        if (playerGuns.ContainsKey(index))
        {
            Debug.Log("Slot already occupied");
            return;
        }

        foreach (var gun in playerGuns.Values)
        {
            if (gun.GetGunType() == gunPrefab.GetGunType())
            {
                Debug.Log("Player already has this weapon:" + gunPrefab.GetGunType());
                return;
            }
        }

        PlayerGun newGun = Instantiate(gunPrefab, weaponHolder);
        newGun.transform.localPosition = Vector3.zero;
        newGun.isActive = false;
        newGun.gameObject.name = gunPrefab.name;

        playerGuns.Add(index, newGun);

        if (currentGunIndex == -1)
        {
            currentGunIndex = index;
            ActivateGun(index);
        }
    }

    public void SwitchWeapon(int index)
    {
        if (!playerGuns.ContainsKey(index)) return;

        if (currentGunIndex != -1)
            playerGuns[currentGunIndex].isActive = false;

        currentGunIndex = index;

        ActivateGun(index);
    }

    private void ActivateGun(int index)
    {
        playerGuns[index].isActive = true;
    }

    public PlayerGun GetCurrentGun()
    {
        if (currentGunIndex < 0) return null;

        return playerGuns[currentGunIndex];
    }

    public bool TryConsumeAmmo(AmmoType type, int amount)
    {
        return ammunitionData.ConsumeAmmo(type, amount);
    }

    public int GetAmmo(AmmoType type)
    {
        return ammunitionData.GetAmmo(type);
    }

    public void AddAmmo(AmmoType type, int amount)
    {
        ammunitionData.AddAmmo(type, amount);
    }

    public void DeactivateAll()
    {
        foreach (var item in playerGuns)
        {
            item.Value.isActive = false;
        }
    }
}