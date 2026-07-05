using System.Collections.Generic;
using CharacterSystem.Data;
using Core.Logging;
using Narrative.Facts.Core;
using World.Races.Core;

namespace World.Races.Integration
{
    /// <summary>
    /// The single writer of the passport fact <c>faction.&lt;raceId&gt;.reads_as_tier</c>
    /// (races-passport.md): projects the hero's equipped parts through their race tags into a
    /// per-race acceptance tier, for EVERY roster race each pass — so a race whose last marker
    /// was swapped away drops back to 0. Mirrors the <c>BiomeStretchDirector</c> single-source
    /// convention; idempotent, and synchronous so the tier is fresh before the next encounter
    /// reads it (FR7).
    /// </summary>
    public sealed class RacePassportProjector
    {
        private readonly IRaceRoster _roster;
        private readonly IPartCatalog _partCatalog;
        private readonly IFactStore _facts;
        private readonly IGameLogger _logger;
        private readonly List<string> _equippedRaceIds = new List<string>();

        public RacePassportProjector(
            IRaceRoster roster, IPartCatalog partCatalog, IFactStore facts, IGameLogger logger = null)
        {
            _roster = roster;
            _partCatalog = partCatalog;
            _facts = facts;
            _logger = logger;
        }

        /// <summary>Recomputes and publishes every roster race's tier from the equipped part ids.</summary>
        public void Recompute(IEnumerable<string> equippedPartIds)
        {
            _equippedRaceIds.Clear();
            if (equippedPartIds != null)
            {
                foreach (var partId in equippedPartIds)
                {
                    if (!_partCatalog.TryGet(partId, out var part))
                    {
                        _logger?.Warning(LogCategory.Narrative,
                            $"[RacePassportProjector] Equipped part id '{partId}' not in the catalog; treated as kindless.");
                        continue;
                    }

                    _equippedRaceIds.Add(part.RaceId);
                }
            }

            var tiers = RaceAcceptanceCalculator.ComputeTiers(_equippedRaceIds, _roster, _logger);
            for (int i = 0; i < _roster.All.Count; i++)
            {
                var raceId = _roster.All[i].Id;
                _facts.SetInt(FactionFacts.ReadsAsTier, tiers[raceId], raceId);
            }
        }
    }
}
