using System;
using System.Collections.Generic;
using Core.Logging;

namespace World.Races.Core
{
    /// <summary>
    /// Id-keyed catalog over the authored races. Duplicate ids are ignored with a warning (first
    /// authored wins), matching the biome appearance/monster-pool catalog convention.
    /// </summary>
    public sealed class RaceRoster : IRaceRoster
    {
        private readonly List<RaceData> _ordered = new List<RaceData>();
        private readonly Dictionary<string, RaceData> _byId =
            new Dictionary<string, RaceData>(StringComparer.Ordinal);

        public RaceRoster(IEnumerable<RaceData> races, IGameLogger logger = null)
        {
            if (races == null)
            {
                return;
            }

            foreach (var race in races)
            {
                if (race == null || string.IsNullOrEmpty(race.Id))
                {
                    continue;
                }

                if (_byId.ContainsKey(race.Id))
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[RaceRoster] Duplicate race id '{race.Id}' ignored; first authored wins.");
                    continue;
                }

                _byId.Add(race.Id, race);
                _ordered.Add(race);
            }
        }

        public IReadOnlyList<RaceData> All => _ordered;

        public bool Contains(string raceId) => !string.IsNullOrEmpty(raceId) && _byId.ContainsKey(raceId);

        public bool TryGet(string raceId, out RaceData race)
        {
            if (string.IsNullOrEmpty(raceId))
            {
                race = null;
                return false;
            }

            return _byId.TryGetValue(raceId, out race);
        }
    }
}
