using UnityEngine;
public class EnemyRotator : MonoBehaviour
{
    private Transform target;
    void Start(){
        target = PlayerController.Instance.transform;
    }
    void Update(){
        Vector3 modifiedTarget = target.position;
        //modifiedTarget.y = transform.position.y;

        transform.LookAt(modifiedTarget);
    }
}
