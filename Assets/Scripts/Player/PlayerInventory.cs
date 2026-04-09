using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public const int MAX_ITEM_AMOUNT = 64;
    public const int MIN_ITEM_AMOUNT = 8;
    public int currentItemAmount;
    public List<PlayerItem> items = new List<PlayerItem>();

    private static PlayerInventory _instance;
    public static PlayerInventory Instance => _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Debug.Log("!");
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    void Start()
    {
        currentItemAmount = MIN_ITEM_AMOUNT;
    }

    public bool AddItem(PlayerItem item)
    {
        if (items.Count >= currentItemAmount)
        {
            LogInterface.Add("NO FREE SLOTS IN INVENTORY");
            return false;
        }

        items.Add(item);
        RebuildIndices();
        LogInterface.Add($"Picked up: {item.itemName}", Color.cyan);
        return true;
    }

    public void RemoveItem(PlayerItem item)
    {
        items.Remove(item);
        RebuildIndices();
    }

    public void RemoveItem(int id)
    {
        if (id >= 0 && id < items.Count)
        {
            items.RemoveAt(id);
            RebuildIndices();
        }
    }

    // Всегда вызывать после любого изменения списка
    void RebuildIndices()
    {
        for (int i = 0; i < items.Count; i++)
            items[i].idInInventory = i;
    }

    public bool HasItems => items.Count > 0;
}