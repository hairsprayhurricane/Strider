using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MedkitItem : PlayerItem
{
    public override void Action()
    {
        PlayerController.Instance.playerHealth.HealPlayerInstant();
    }   
}