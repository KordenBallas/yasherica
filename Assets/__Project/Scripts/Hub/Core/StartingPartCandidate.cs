namespace Hub.Core
{
    /// <summary>
    /// One part eligible for the Hub's starting offer (O1): a tasted, base-skeleton-fitting part
    /// with the two axes the pick trades on — the race lean (empty = kindless) and whether it
    /// brings an opening move. UnityEngine-free; built by the Data layer from the tasted catalog.
    /// </summary>
    public sealed class StartingPartCandidate
    {
        public StartingPartCandidate(string partId, string raceId, string slotId, bool hasActiveAbility)
        {
            PartId = partId ?? string.Empty;
            RaceId = raceId ?? string.Empty;
            SlotId = slotId ?? string.Empty;
            HasActiveAbility = hasActiveAbility;
        }

        public string PartId { get; }

        /// <summary>The race marker the part carries; empty = kindless scrap.</summary>
        public string RaceId { get; }

        public string SlotId { get; }

        /// <summary>True when the part grants at least one active ability (an opening move).</summary>
        public bool HasActiveAbility { get; }
    }
}
