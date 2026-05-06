using UnityEngine;

public abstract class PlayerItem : ScriptableObject
{
        public MeshRenderer model;

    [Header("Info")]
    public string itemName = "Unknown Item";
    public Sprite itemIcon;
    [TextArea] public string itemDescription = "";

    [HideInInspector] public int idInInventory = 0;

    [Header("Throw")]
    public bool  isThrowable  = false;
    public float throwSpeed   = 15f;
    public float throwGravity = -25f;

    public void Use() { Action(); Consume(); }

    public void UseAtPoint(Vector3 point) { ActionAtPoint(point); Consume(); }

    public abstract void Action();

    public virtual void ActionAtPoint(Vector3 point) { Action(); }

    public void Consume() { PlayerInventory.Instance.RemoveItem(this); }
}