using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EnemyType : MonoBehaviour
{
    public string type;
    public bool isDied;
    public enum EnemyClass
    {
        Unknown = 0,
        Ranger = 1,
        Shotgunner = 2,
        Assaultron = 3,
    }
    public enum EnemyDifficultyFilter
    {
        AlwaysPresent = 0,    // Always Present
        Easy = 1,             // Present on difficulties starting with Easy
        Standard = 2,         // Present on difficulties starting with Standard
        Hard = 3,             // Present on difficulties starting with Hard
        Nightmare = 4         // Present only on Nightmare.
    }

    public EnemyDifficultyFilter difficultyFilter;
    public EnemyClass classType;
    public static List<EnemyType> enemiesList = new List<EnemyType>();
    public Enemy enemy;
    public static bool destroyAllNewInstantiatedEnemies = false;
    void Awake()
    {
        if (destroyAllNewInstantiatedEnemies)
        {
            Destroy(gameObject);
        }
        
        enemy = GetComponentInChildren<Enemy>();

        enemiesList.Add(this);
    }

    public static string getEnemyPath(string classType)
    {
        return "Prefabs/Enemies/" + classType;
    }
    public string getEnemyPath()
    {
        return "Prefabs/Enemies/" + classType;
    }
    
    void OnDestroy()
    {
        if (enemiesList.Contains(this))
        {
            enemiesList.Remove(this);
        }
    }
}
