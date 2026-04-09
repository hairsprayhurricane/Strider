using UnityEngine;

public abstract class PlayerItem : ScriptableObject
{
    [Header("Info")]
    public string itemName = "Unknown Item";
    public Sprite itemIcon;
    [TextArea] public string itemDescription = "";

    [HideInInspector] public int idInInventory = 0;

    public void Use()
    {
        Action();
        AfterAction();
    }

    public abstract void Action();

    private void AfterAction()
    {
        PlayerInventory.Instance.RemoveItem(this);
    }
}