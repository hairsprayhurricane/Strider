using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class RedBarrel : MonoBehaviour
{
    public int id;
    public int health = 20;
    public static List<RedBarrel> barrels = new List<RedBarrel>();
    public Collider colliderChildren;

    void Awake()
    {
        barrels.Add(this);
    }

    public void Boom()
    {
        Explosion.CreateExplosion(20, transform);
        barrels.Remove(this);
        Destroy(colliderChildren);
        Destroy(gameObject);
    }

    public void Boom(float delay)
    {
        BoomDelay(delay);
    }

    private IEnumerator BoomDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        Boom();
    }

    void OnDestroy()
    {
        if (barrels != null)
        {
            barrels.Remove(this);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        /*if (other.CompareTag("Enemy") && other.GetComponent<Enemy>().isDead)
        {
            Boom();
        }*/
    }
}
