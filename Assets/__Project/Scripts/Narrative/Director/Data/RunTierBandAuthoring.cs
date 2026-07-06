using System;
using Narrative.Director.Core;
using UnityEngine;

namespace Narrative.Director.Data
{
    /// <summary>
    /// Authoring form of a <see cref="RunTierBand"/>: the run-escalation tier range at which a story or
    /// monster is eligible. <c>_minTier</c> opens the content upward; <c>_maxTier &lt;= 0</c> means no
    /// upper bound. The default <c>(0, 0)</c> is an open band (eligible at every tier), so a field left
    /// unauthored — or absent from an older asset — keeps that content eligible everywhere (no migration).
    ///
    /// The three named registers (Backwater / Courts / Divine-Apex) are a documented tier-range
    /// convention over the 1-based tier, not a distinct type: an author picks the min/max for the band.
    /// </summary>
    [Serializable]
    public class RunTierBandAuthoring
    {
        [Tooltip("Lowest run tier at which this content is eligible; content opens upward from here (1-based)")]
        [Min(0)]
        [SerializeField] private int _minTier;

        [Tooltip("Highest eligible run tier; 0 (or less) means no upper bound. Use sparingly to age out low content high up")]
        [SerializeField] private int _maxTier;

        public RunTierBandAuthoring()
        {
        }

        public RunTierBandAuthoring(int minTier, int maxTier)
        {
            _minTier = minTier;
            _maxTier = maxTier;
        }

        public RunTierBand ToCore() => new RunTierBand(_minTier, _maxTier);
    }
}
