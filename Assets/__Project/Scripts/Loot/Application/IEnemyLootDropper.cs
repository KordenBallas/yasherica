using Platform;

namespace Loot.Application
{
    /// <summary>
    /// Spawns artifact drops at the death locations of defeated enemies on a
    /// platform. Called when combat ends, before enemy GameObjects are destroyed.
    /// </summary>
    public interface IEnemyLootDropper
    {
        void DropFor(IPlatform platform, bool playerWon);
    }
}
