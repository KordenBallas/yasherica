using System.Collections.Generic;

namespace Hub.Core
{
    /// <summary>
    /// One part eligible for the Hub's starting offer (O1): a tasted, base-skeleton-fitting part
    /// with the axes the pick trades on — the race lean (empty = kindless), whether it brings an
    /// opening move, its function-trait leans, and its authored draw weight once unlocked
    /// (Track R). UnityEngine-free; built by the Data layer from the tasted catalog.
    /// </summary>
    public sealed class StartingPartCandidate
    {
        private static readonly IReadOnlyList<string> NoTraits = new string[0];

        public StartingPartCandidate(
            string partId,
            string raceId,
            string slotId,
            bool hasActiveAbility,
            IReadOnlyList<string> traitIds = null,
            float drawWeight = 1f)
        {
            PartId = partId ?? string.Empty;
            RaceId = raceId ?? string.Empty;
            SlotId = slotId ?? string.Empty;
            HasActiveAbility = hasActiveAbility;
            TraitIds = traitIds ?? NoTraits;
            DrawWeight = drawWeight > 0f ? drawWeight : 1f;
        }

        public string PartId { get; }

        /// <summary>The race marker the part carries; empty = kindless scrap.</summary>
        public string RaceId { get; }

        public string SlotId { get; }

        /// <summary>True when the part grants at least one active ability (an opening move).</summary>
        public bool HasActiveAbility { get; }

        /// <summary>Trait affinities' ids — the artifact-family axis the direction bias matches on.</summary>
        public IReadOnlyList<string> TraitIds { get; }

        /// <summary>Authored per-token draw weight (Track R); 1 = neutral.</summary>
        public float DrawWeight { get; }
    }
}
