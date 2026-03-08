using UnityEngine;

public class SpriteRotator : MonoBehaviour
{
    private Transform target;

    void Awake()    {target = Camera.main.transform;}
    void Update()   {transform.LookAt(target);}
}
