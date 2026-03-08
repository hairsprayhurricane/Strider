using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class WeaponPickup : MonoBehaviour
{
    public byte index = 0;
    public PlayerGun gunPrefab;

    private void OnTriggerEnter(Collider other)
    {
        GunController gunController = other.GetComponent<GunController>();

        if (gunController != null)
        {
            gunController.AddWeapon(gunPrefab, index);
            Destroy(gameObject);
        }
    }
}
