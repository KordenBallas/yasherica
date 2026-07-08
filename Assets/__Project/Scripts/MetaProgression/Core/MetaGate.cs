using System.Collections.Generic;
using Narrative.Facts.Core;

namespace MetaProgression.Core
{
    /// <summary>
    /// Immutable per-token gating record (meta-progression FR2/FR4): the gating mark, the deed that
    /// earns the token (an AND-list of authored fact predicates over the cross-run meta store —
    /// the same precondition grammar stories and threads use, so adding a deed channel is data
    /// only), the unlock tier (indexes the pacing run-floors, FR11/FR12), and the relative draw
    /// weight once unlocked. A <see cref="GatingMark.MetaGated"/> token with an empty deed is gated
    /// by its tier run-floor alone — consistent with the precondition grammar, where an empty
    /// predicate list always passes. A gate may additionally demand a minimum total Heat
    /// (heat-ascension FR5), keyed off the current pact or the persisted hottest clear.
    /// </summary>
    public sealed class MetaGate
    {
        private static readonly IReadOnlyList<FactPredicate> EmptyDeed = new FactPredicate[0];

        /// <summary>The default gate every unmarked token carries (FR15): base, no deed, tier 0.</summary>
        public static readonly MetaGate Base = new MetaGate(GatingMark.Base, null, 0, 1f);

        public GatingMark Mark { get; }
        public IReadOnlyList<FactPredicate> Deed { get; }
        public int UnlockTier { get; }
        public float DrawWeight { get; }

        /// <summary>Minimum total Heat this token demands; 0 = no Heat gate (the default).</summary>
        public int MinHeat { get; }

        /// <summary>Which Heat reading <see cref="MinHeat"/> compares against.</summary>
        public HeatGateKey HeatKey { get; }

        public MetaGate(
            GatingMark mark,
            IReadOnlyList<FactPredicate> deed,
            int unlockTier,
            float drawWeight,
            int minHeat = 0,
            HeatGateKey heatKey = HeatGateKey.CurrentPact)
        {
            Mark = mark;
            Deed = deed ?? EmptyDeed;
            UnlockTier = unlockTier < 0 ? 0 : unlockTier;
            DrawWeight = drawWeight > 0f ? drawWeight : 1f;
            MinHeat = minHeat < 0 ? 0 : minHeat;
            HeatKey = heatKey;
        }
    }
}
