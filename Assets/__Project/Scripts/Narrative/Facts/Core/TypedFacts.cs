using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Curated, compile-safe references to world-namespace fact keys. Source of truth remains the SO
    /// registry; these mirror a hand-picked subset for code that needs type-safe reads/writes.
    /// </summary>
    public static class WorldFacts
    {
        public static readonly FactKeyRef BarnRaided = new FactKeyRef(FactNamespace.World, FactScope.Global, "barn_raided", FactValueType.Bool);
        public static readonly FactKeyRef GrainRecovered = new FactKeyRef(FactNamespace.World, FactScope.Global, "grain_recovered", FactValueType.Bool);

        /// <summary>
        /// Authored escalation tier of the run's active biome stretch. Published by the biome journey
        /// (<c>BiomeStretchDirector</c>); a seam for D19 — nothing consumes it for difficulty/tone yet.
        /// </summary>
        public static readonly FactKeyRef RunEscalationTier = new FactKeyRef(FactNamespace.World, FactScope.Global, "run_escalation_tier", FactValueType.Int);
    }

    /// <summary>Curated, compile-safe references to per-actor fact keys.</summary>
    public static class ActorFacts
    {
        public static readonly FactKeyRef LootedBarn = new FactKeyRef(FactNamespace.Actor, FactScope.PerActor, "looted_barn", FactValueType.Bool);
    }

    /// <summary>Curated, compile-safe references to per-faction fact keys.</summary>
    public static class FactionFacts
    {
        /// <summary>
        /// The passport (races-passport.md): per-race acceptance tier, subject = race id
        /// (0 outsider / 1 tolerated / 2 kin). Derived from the equipped body's race-tagged parts;
        /// written only by <c>RacePassportProjector</c>. Preconditions gate with a literal race-id
        /// subject token, e.g. <c>faction.ibex.reads_as_tier &gt;= 2</c>.
        /// </summary>
        public static readonly FactKeyRef ReadsAsTier = new FactKeyRef(FactNamespace.Faction, FactScope.PerFaction, "reads_as_tier", FactValueType.Int);
    }

    /// <summary>Enumerates every curated <see cref="FactKeyRef"/> for the drift check (D3).</summary>
    public static class TypedFacts
    {
        public static IEnumerable<FactKeyRef> All()
        {
            yield return WorldFacts.BarnRaided;
            yield return WorldFacts.GrainRecovered;
            yield return WorldFacts.RunEscalationTier;
            yield return ActorFacts.LootedBarn;
            yield return FactionFacts.ReadsAsTier;
        }
    }
}
