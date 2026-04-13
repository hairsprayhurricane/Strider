using System;
using System.Collections.Generic;
using UnityEngine;

// FSM means Finite State Machine
public class EnemyFSM : MonoBehaviour
{
    public enum GlobalState
    {
        Calm,
        Search,
        Attack
    }

    public static GlobalState CurrentState { get; private set; } = GlobalState.Calm;
    public static event Action<GlobalState> OnStateChanged;

    public static void SetCalm()
    {
        if (CurrentState == GlobalState.Attack) return;
        TransitionTo(GlobalState.Calm);
    }

    public static void SetAttack()
    {
        TransitionTo(GlobalState.Attack);
    }

    /// Transitions all enemies to Search state without a specific target.
    public static void GlobalPlayerSearch()
    {
        if (CurrentState == GlobalState.Attack) return;
        TransitionTo(GlobalState.Search);
    }

    /// Sends the 1-2 closest non-aggro enemies to investigate the given position.
    public static void GlobalPlayerSearch(Vector3 position)
    {
        if (CurrentState == GlobalState.Attack) return;
        TransitionTo(GlobalState.Search);

        var candidates = new List<(EnemyAi ai, float dist)>();
        foreach (var et in EnemyType.enemiesList)
        {
            if (et.enemy == null) continue;
            EnemyAi ai = et.enemy.enemyAi;
            EnemyAwareness awareness = et.enemy.enemyAwareness;
            if (ai == null || (awareness != null && awareness.isAggro)) continue;

            float dist = Vector3.Distance(et.enemy.transform.position, position);
            candidates.Add((ai, dist));
        }

        if (candidates.Count == 0) return;

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        int sendCount = Mathf.Min(UnityEngine.Random.Range(1, 3), candidates.Count);
        for (int i = 0; i < sendCount; i++)
            candidates[i].ai.InvestigatePoint(position);
    }

    /// Checks if any enemy is within <paramref name="radius"/> units of <paramref name="position"/>.
    /// If found, logs its name and triggers a global search. Uses sqrMagnitude — no sqrt overhead.
    public static void CheckClosestEnemy(Vector3 position, float radius)
    {
        float sqrRadius = radius * radius;
        foreach (var et in EnemyType.enemiesList)
        {
            if (et.enemy == null) continue;
            if (et.enemy.isDead) continue;
            
            if ((et.enemy.transform.position - position).sqrMagnitude <= sqrRadius)
            {
                Debug.Log($"[EnemyFSM] Nearby enemy detected: {et.enemy.gameObject.name}");
                GlobalPlayerSearch(position);
                return;
            }
        }
    }

    /// Sends ALL non-aggro enemies within <paramref name="radius"/> to investigate <paramref name="position"/>.
    /// Used for loud gunshots — every enemy in earshot turns toward the source.
    public static void AlertNearbyEnemies(Vector3 position, float radius)
    {
        if (CurrentState == GlobalState.Attack) return;

        float sqrRadius  = radius * radius;
        bool  anyAlerted = false;

        foreach (var et in EnemyType.enemiesList)
        {
            if (et.enemy == null || et.enemy.isDead) continue;

            EnemyAi        ai        = et.enemy.enemyAi;
            EnemyAwareness awareness = et.enemy.enemyAwareness;

            if (ai == null || (awareness != null && awareness.isAggro)) continue;

            if ((et.enemy.transform.position - position).sqrMagnitude <= sqrRadius)
            {
                ai.InvestigatePoint(position);
                anyAlerted = true;
            }
        }

        if (anyAlerted)
            TransitionTo(GlobalState.Search);
    }

    /// Checks if all enemies are dead. If so, resets FSM to Calm and switches music to stealth.
    public static void CheckAllCleared()
    {
        foreach (var et in EnemyType.enemiesList)
            if (!et.isDied) return;

        TransitionTo(GlobalState.Calm);
        if (ControllerOST.Instance != null) ControllerOST.Instance.PlayStealth();
    }

    private static void TransitionTo(GlobalState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}
