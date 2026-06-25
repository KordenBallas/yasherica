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
    }

    /// <summary>Curated, compile-safe references to per-actor fact keys.</summary>
    public static class ActorFacts
    {
        public static readonly FactKeyRef LootedBarn = new FactKeyRef(FactNamespace.Actor, FactScope.PerActor, "looted_barn", FactValueType.Bool);
    }

    /// <summary>Enumerates every curated <see cref="FactKeyRef"/> for the drift check (D3).</summary>
    public static class TypedFacts
    {
        public static IEnumerable<FactKeyRef> All()
        {
            yield return WorldFacts.BarnRaided;
            yield return WorldFacts.GrainRecovered;
            yield return ActorFacts.LootedBarn;
        }
    }
}
