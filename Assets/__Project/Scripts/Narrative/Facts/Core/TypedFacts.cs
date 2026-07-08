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
        /// Effective escalation tier of the run's active biome stretch (D19): the authored stretch
        /// tier plus any Heat lift (Track Y). Published by the biome journey
        /// (<c>BiomeStretchDirector</c>); consumed by the window planner's story tier-bands and the
        /// ambient/site monster pool draw (<c>RunWindowPlanner</c>).
        /// </summary>
        public static readonly FactKeyRef RunEscalationTier = new FactKeyRef(FactNamespace.World, FactScope.Global, "run_escalation_tier", FactValueType.Int);

        /// <summary>
        /// Meta-horizon hottest cleared total Heat (Track Y FR4) — the mastery record min-Heat gates
        /// may key off. Written only by <c>HeatHighWaterRecorder</c> when a hot run survives to the
        /// clear-window savepoint; never lowered.
        /// </summary>
        public static readonly FactKeyRef HeatHighWater = new FactKeyRef(FactNamespace.World, FactScope.Global, "heat_high_water", FactValueType.Int);

        /// <summary>
        /// Meta-horizon counter of runs started (1 on the first run; a continue is not a new run).
        /// Written only by <c>RunCounterService</c>; spine soft floors gate on it ("not before run
        /// N", D7/P3-1).
        /// </summary>
        public static readonly FactKeyRef RunCount = new FactKeyRef(FactNamespace.World, FactScope.Global, "run_count", FactValueType.Int);

        /// <summary>
        /// Meta-horizon cross-run spine cursor (D20/P3-3), subject = story id: true once the player
        /// has actually SEEN the reveal-beat (its dialogue ran to an outcome, walk-away included) —
        /// not merely had it placed. Written only by <c>SpineSeenRecorder</c>; the reserved lane
        /// excludes a seen beat from the spine pool in every later run.
        /// </summary>
        public static readonly FactKeyRef SpineSeen = new FactKeyRef(FactNamespace.World, FactScope.PerStory, "spine_seen", FactValueType.Bool);

        /// <summary>
        /// Meta-horizon tasted-forms catalog (P4-5), subject = part id: true once the hero has
        /// carried the part in any run (equipped or dormant). Written only by
        /// <c>TastedFormsRecorder</c>; read only by the Arena draft to widen the shared board.
        /// Passive unlock — no currency, no achievement gate, never feeds back into Journey.
        /// </summary>
        public static readonly FactKeyRef ArenaTasted = new FactKeyRef(FactNamespace.World, FactScope.PerPart, "arena_tasted", FactValueType.Bool);

        /// <summary>
        /// Count of Monster-verb kills this run (P1-7): incremented by
        /// <c>MonsterVerbConsequences</c> whenever a talkable NPC dies in a dialogue-routed fight.
        /// The Conquest path-lean the passport gating and the cauldron bark register read.
        /// </summary>
        public static readonly FactKeyRef PathConquest = new FactKeyRef(FactNamespace.World, FactScope.Global, "path_conquest", FactValueType.Int);

        /// <summary>
        /// Count of restrained choices this run (P1-10): incremented when the player takes a modest /
        /// marker (passport) part. The friendship/restraint counterweight to
        /// <see cref="PathConquest"/> — the bark register sours as this side leads. No new meter:
        /// the lean is only ever read by comparing the two counters.
        /// </summary>
        public static readonly FactKeyRef PathRestraint = new FactKeyRef(FactNamespace.World, FactScope.Global, "path_restraint", FactValueType.Int);
    }

    /// <summary>Curated, compile-safe references to per-actor fact keys.</summary>
    public static class ActorFacts
    {
        public static readonly FactKeyRef LootedBarn = new FactKeyRef(FactNamespace.Actor, FactScope.PerActor, "looted_barn", FactValueType.Bool);

        /// <summary>
        /// Set once when the actor dies in a dialogue-routed fight (the Monster verb, P1-7). Written
        /// only by <c>MonsterVerbConsequences</c>; the planner reads it to never recast a slain actor,
        /// and authored preconditions may gate on it (a door a killing closes).
        /// </summary>
        public static readonly FactKeyRef Slain = new FactKeyRef(FactNamespace.Actor, FactScope.PerActor, "slain", FactValueType.Bool);
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
            yield return WorldFacts.HeatHighWater;
            yield return WorldFacts.RunCount;
            yield return WorldFacts.SpineSeen;
            yield return WorldFacts.ArenaTasted;
            yield return WorldFacts.PathConquest;
            yield return WorldFacts.PathRestraint;
            yield return ActorFacts.LootedBarn;
            yield return ActorFacts.Slain;
            yield return FactionFacts.ReadsAsTier;
        }
    }
}
