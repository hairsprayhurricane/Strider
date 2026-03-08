using System;
using System.Collections.Generic;
using UnityEngine;
public enum AmmoType
{
    Lead,
    BuckShot,
    Rocket,
    Energetic,

}
[Serializable]
public class AmmoEntry
{
    public AmmoType type;
    public int amount;
    public int maxAmount;
}
[Serializable]
public class AmmunitionData
{
    public List<AmmoEntry> ammoEntries = new List<AmmoEntry>()
    {
        new AmmoEntry { type = AmmoType.Lead, amount = 300, maxAmount = 300 },
        new AmmoEntry { type = AmmoType.BuckShot, amount = 50, maxAmount = 50 },
        new AmmoEntry { type = AmmoType.Rocket, amount = 25, maxAmount = 25 },
        new AmmoEntry { type = AmmoType.Energetic, amount = 1000, maxAmount = 1000 }
    };

    public int GetAmmo(AmmoType type)
    {
        foreach (var entry in ammoEntries)
        {
            if (entry.type == type)
                return entry.amount;
        }

        return 0;
    }

    public bool ConsumeAmmo(AmmoType type, int amount)
    {
        foreach (var entry in ammoEntries)
        {
            if (entry.type == type)
            {
                if (entry.amount < amount)
                    return false;

                entry.amount -= amount;
                return true;
            }
        }

        return false;
    }

    public void AddAmmo(AmmoType type, int amount)
    {
        foreach (var entry in ammoEntries)
        {
            if (entry.type == type)
            {
                entry.amount += amount;

                if (entry.amount > entry.maxAmount)
                    entry.amount = entry.maxAmount;

                return;
            }
        }
    }
}
