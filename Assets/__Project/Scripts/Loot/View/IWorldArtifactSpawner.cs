using System;
using Loot.Core;
using UnityEngine;

namespace Loot.View
{
    /// <summary>
    /// Creates world artifact pickups (view + presenter) at a position.
    /// onCollected fires after the artifact lands in the inventory.
    /// </summary>
    public interface IWorldArtifactSpawner
    {
        void Spawn(LootRollResult loot, Vector3 position, Action onCollected = null);
    }
}
