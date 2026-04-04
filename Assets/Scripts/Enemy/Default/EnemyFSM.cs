using System;
using System.Collections.Generic;
using UnityEngine;

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

    private static void TransitionTo(GlobalState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
    }
}
