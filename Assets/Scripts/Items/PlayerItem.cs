using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public abstract class PlayerItem
{
    public byte idInInventory = 0;
    public void Use()
    {
        Action();
        AfterAction();
    }   

    public abstract void Action();
    public void AfterAction()
    {
        PlayerInventory.Instance.RemoveItem(idInInventory);   
    }
}