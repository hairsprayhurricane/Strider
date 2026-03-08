using System.Collections.Generic;
using UnityEngine;

public class EnemyBloodCellBehavior : MonoBehaviour
{
    const int MaxCells = 2000;
    private static Queue<GameObject> cellQueue = new Queue<GameObject>();

    void Start()
    {
        if (ClassicRandom.value < 0.5f)
            Destroy(gameObject, 10f);

        cellQueue.Enqueue(gameObject);

        if (cellQueue.Count > MaxCells)
            TrimOldestCell();
    }

    private void TrimOldestCell()
    {
        while (cellQueue.Count > 0)
        {
            var oldest = cellQueue.Dequeue();
            if (oldest != null)
            {
                Destroy(oldest);
                break;
            }

        }
    }
}
