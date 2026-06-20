using System.Collections.Generic;

namespace Narrative.Facts.Core
{
    /// <summary>
    /// Curated, compile-safe references to world-namespace fact keys. Source of truth remains the SO
    /// registry; these mirror a hand-picked subset for code that needs type-safe reads/writes.
    /// </summary>
    public static class WorldFacts
    {
        public static readonly FactKeyRef PassBlocked = new FactKeyRef(FactNamespace.World, FactScope.Global, "pass_blocked", FactValueType.Bool);
        public static readonly FactKeyRef PassCleared = new FactKeyRef(FactNamespace.World, FactScope.Global, "pass_cleared", FactValueType.Bool);
    }

    /// <summary>Curated, compile-safe references to per-actor fact keys.</summary>
    public static class ActorFacts
    {
        public static readonly FactKeyRef Hostile = new FactKeyRef(FactNamespace.Actor, FactScope.PerActor, "hostile", FactValueType.Bool);
    }

    /// <summary>Curated, compile-safe references to per-faction fact keys.</summary>
    public static class FactionFacts
    {
        public static readonly FactKeyRef Reputation = new FactKeyRef(FactNamespace.Faction, FactScope.PerFaction, "reputation", FactValueType.Int);
    }

    /// <summary>Enumerates every curated <see cref="FactKeyRef"/> for the drift check (D3).</summary>
    public static class TypedFacts
    {
        public static IEnumerable<FactKeyRef> All()
        {
            yield return WorldFacts.PassBlocked;
            yield return WorldFacts.PassCleared;
            yield return ActorFacts.Hostile;
            yield return FactionFacts.Reputation;
        }
    }
}
