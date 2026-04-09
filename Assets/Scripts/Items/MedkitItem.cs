using UnityEngine;

[CreateAssetMenu(fileName = "MedkitItem", menuName = "Items/Medkit")]
public class MedkitItem : PlayerItem
{
    public MeshRenderer model;
    public override void Action()
    {
        PlayerController.Instance.playerHealth.HealPlayerInstant();
    }   
}