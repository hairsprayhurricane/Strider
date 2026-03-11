using System;
using UnityEngine;
using System.Threading.Tasks;


public class EnemyManager : MonoBehaviour
{
    public static async void KillAll()
    {    
        await Awaitable.MainThreadAsync();

        foreach (var enemyType in EnemyType.enemiesList)
        {
            //KelerMarker.Instance.CallKelerOnEnemy(enemyType.enemy, 1);
        }
    }

    public static async Task DestroyAllAIAsync()
    {
        await Awaitable.MainThreadAsync();

        foreach (var enemyType in EnemyType.enemiesList)
        {
            EnemyAi enemyAi = enemyType.enemy.enemyAi;
            enemyAi.ResetPathForNav();
            Debug.Log(enemyAi);
            Destroy(enemyAi);
            enemyType.enemy.enemyAwareness.isAggro = false;
        }
    }
    public static void DestroyAllAI()
    {
        foreach (var enemyType in EnemyType.enemiesList)
        {
            EnemyAi enemyAi = enemyType.enemy.enemyAi;
            enemyAi.ResetPathForNav();
            Destroy(enemyAi);
            enemyType.enemy.enemyAwareness.isAggro = false;
        }
    }
    public static void AggroAll()
    {
        foreach (var enemyType in EnemyType.enemiesList)
        {
            var enemyAwareness = enemyType.enemy.enemyAwareness;
            if (enemyAwareness) enemyAwareness.isAggro = true;
        }
    }
}



