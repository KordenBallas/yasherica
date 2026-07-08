using System.Collections.Generic;
using Narrative.Facts.Core;
using Narrative.Runtime.Snapshots;

namespace MetaProgression.Core
{
    /// <summary>
    /// Default <see cref="IMetaVocabulary"/>: a frozen image of the meta facts plus the tuning
    /// settings. Deterministic (FR13 — same snapshot + settings ⇒ identical answers) and fail-safe
    /// (FR14 — a null/empty snapshot degrades to base-only, never throws).
    ///
    /// The effective run count is supplied by the caller as "the run this vocabulary serves"
    /// (the Hub serves the upcoming run = stored count + 1; a restored run serves the stored
    /// count), and it overrides the snapshot's <c>world.run_count</c> entry so authored
    /// run-counter deeds and the tier run-floors read the same number.
    /// </summary>
    public sealed class MetaVocabulary : IMetaVocabulary
    {
        private readonly Dictionary<FactKey, FactValue> _facts;
        private readonly MetaProgressionSettings _settings;
        private readonly int _effectiveRunCount;
        private readonly IHeatLens _heat;

        public MetaVocabulary(
            FactStoreSnapshot metaFacts,
            MetaProgressionSettings settings,
            int effectiveRunCount,
            IHeatLens heat = null)
        {
            _settings = settings ?? MetaProgressionSettings.Defaults;
            _effectiveRunCount = effectiveRunCount < 0 ? 0 : effectiveRunCount;
            _heat = heat;
            _facts = BuildFacts(metaFacts, _effectiveRunCount);
        }

        public bool IsUnlocked(string tokenId, MetaGate gate)
        {
            if (gate == null || gate.Mark == GatingMark.Base)
            {
                return true;
            }

            // Heat lens absent ⇒ readings are 0: min-Heat gates fail closed, floors get no relief —
            // a scene without Heat installed answers exactly as before Track Y (FR13's Heat-0 rule).
            if (gate.MinHeat > 0)
            {
                int measured = gate.HeatKey == HeatGateKey.HighWaterMark
                    ? (_heat?.HighWaterHeat ?? 0)
                    : (_heat?.CurrentHeat ?? 0);
                if (measured < gate.MinHeat)
                {
                    return false;
                }
            }

            int floor = _settings.FloorForTier(gate.UnlockTier) - (_heat?.FloorReliefRuns ?? 0);
            if (_effectiveRunCount < (floor < 0 ? 0 : floor))
            {
                return false;
            }

            return SnapshotPredicateEvaluator.EvaluateAll(gate.Deed, _facts, tokenId ?? string.Empty);
        }

        private static Dictionary<FactKey, FactValue> BuildFacts(FactStoreSnapshot snapshot, int effectiveRunCount)
        {
            var facts = new Dictionary<FactKey, FactValue>();
            if (snapshot?.Entries != null)
            {
                foreach (var entry in snapshot.Entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    facts[new FactKey(entry.Namespace, entry.Subject, entry.Key)] =
                        FactStoreSnapshotMapper.ToValue(entry);
                }
            }

            facts[WorldFacts.RunCount.ToKey()] = FactValue.FromInt(effectiveRunCount);
            return facts;
        }
    }
}
