using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public const byte MAX_ITEM_AMOUNT = 64;
    public const byte MIN_ITEM_AMOUNT = 8;
    public byte currentItemAmount;
    public List<PlayerItem> items = new List<PlayerItem>();

    private static PlayerInventory _instance;
    public static PlayerInventory Instance { get { return _instance; } }

    public void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
    }

    public void Start()
    {
        currentItemAmount = MIN_ITEM_AMOUNT; // this is fine
    }

    public bool AddItem(PlayerItem item)
    {
        if (items.Count >= currentItemAmount)
        {
            LogInterface.Add("NO FREE SLOTS IN INVENTORY");
            return false;
        }

        items.Add(item);
        item.idInInventory = (byte)items.IndexOf(item);
        return true;
    }
    public void RemoveItem (int id)
    {
        items.RemoveAt(id);
    }
    public void RemoveItem (PlayerItem item)
    {
        items.Remove(item);
    }
}