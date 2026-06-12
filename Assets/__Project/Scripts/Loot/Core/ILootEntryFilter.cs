namespace Loot.Core
{
    /// <summary>
    /// Eligibility check applied to loot table entries before weighted picking.
    /// Future progression rules (player level, achievements, quest history,
    /// difficulty scaling) plug in as additional implementations.
    /// </summary>
    public interface ILootEntryFilter
    {
        bool IsEligible(LootEntryData entry, LootRollContext context);
    }
}
