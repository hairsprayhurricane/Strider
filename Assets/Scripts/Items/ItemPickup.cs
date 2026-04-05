using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public PlayerItem itemPrefab;
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if(PlayerInventory.Instance.AddItem(itemPrefab))
            {
                Destroy(gameObject);
            }
        }
    }
}
