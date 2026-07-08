using System;
using System.Collections.Generic;
using MetaProgression.Core;
using Narrative.Facts.Core;
using Narrative.Facts.Data;
using UnityEngine;

namespace MetaProgression.Data
{
    /// <summary>
    /// The per-token meta-gating block (meta-progression FR2/FR15), embedded on every gateable
    /// content SO (parts, artifacts, recipes, blanks). The serialized defaults ARE the unmarked
    /// state — an asset that never touches this block stays base and keeps working. The deed reuses
    /// the authored fact-predicate grammar (<see cref="FactPredicateSerial"/>, AND over the list);
    /// <c>$self</c> binds to the owning token's id, so "taste this very form" needs no literal id.
    /// </summary>
    [Serializable]
    public class MetaGatingAuthoring
    {
        [Tooltip("Base = available from run 1 (the default). Meta Gated = absent from every draw until the deed below is met.")]
        [SerializeField] private GatingMark _mark = GatingMark.Base;

        [Tooltip("The deed that earns this token (AND over the list; empty = the tier run-floor alone gates it). Subject $self = this token's id. MVP channels: world.$self.arena_tasted (taste the form) and reveal-spine facts / world.run_count.")]
        [SerializeField] private List<FactPredicateSerial> _deed = new List<FactPredicateSerial>();

        [Tooltip("Unlock tier (FR12 stratification): indexes the pacing run-floors in MetaProgressionConfig. 0 = small/early token; higher = late run-shaping unlock.")]
        [Min(0)]
        [SerializeField] private int _unlockTier;

        [Tooltip("Relative draw weight once unlocked (the dig's tie-break and any biased draw).")]
        [Min(0.01f)]
        [SerializeField] private float _drawWeight = 1f;

        [Tooltip("Minimum total Heat this token demands (heat-ascension FR5). 0 = no Heat gate (the default); only read on Meta Gated tokens. Reserve for the rarer run-shaping tokens — never the base grammar.")]
        [Min(0)]
        [SerializeField] private int _minHeat;

        [Tooltip("Which Heat reading the min-Heat gate compares against: the run's current pact, or the persisted hottest clear.")]
        [SerializeField] private HeatGateKey _heatKey = HeatGateKey.CurrentPact;

        /// <summary>Maps to the immutable Core gate; a null/default block reads as base.</summary>
        public MetaGate ToCore()
        {
            if (_mark == GatingMark.Base)
            {
                return MetaGate.Base;
            }

            List<FactPredicate> deed = null;
            if (_deed != null && _deed.Count > 0)
            {
                deed = new List<FactPredicate>(_deed.Count);
                foreach (var predicate in _deed)
                {
                    if (predicate != null)
                    {
                        deed.Add(predicate.ToCore());
                    }
                }
            }

            return new MetaGate(_mark, deed, _unlockTier, _drawWeight, _minHeat, _heatKey);
        }
    }
}
