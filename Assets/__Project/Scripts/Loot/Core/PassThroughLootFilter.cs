namespace Loot.Core
{
    /// <summary>
    /// Accepts every entry. The LootEntryData gating fields (MinPlayerLevel,
    /// RequiredAchievements, RequiredPastQuests) stay unenforced until a
    /// progression-aware filter replaces this one.
    /// </summary>
    public class PassThroughLootFilter : ILootEntryFilter
    {
        public bool IsEligible(LootEntryData entry, LootRollContext context)
        {
            return true;
        }
    }
}
