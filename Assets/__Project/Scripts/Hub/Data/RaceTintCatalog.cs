using System;
using System.Collections.Generic;
using UnityEngine;
using World.Races.Data;

namespace Hub.Data
{
    /// <summary>
    /// Race id → belonging colour for the Hub's card tints (O1) — the belonging-colour grammar
    /// (races-passport.md) read straight off the authored <see cref="RaceDefinition"/>s, since
    /// Core race records deliberately hold no Unity colour. Unknown/kindless ids read as white.
    /// </summary>
    public class RaceTintCatalog : IRaceTintCatalog
    {
        private readonly Dictionary<string, Color> _tintByRace =
            new Dictionary<string, Color>(StringComparer.Ordinal);

        public RaceTintCatalog(IReadOnlyList<RaceDefinition> races)
        {
            if (races == null)
            {
                return;
            }

            foreach (var race in races)
            {
                if (race != null && !string.IsNullOrEmpty(race.RaceId)
                    && !_tintByRace.ContainsKey(race.RaceId))
                {
                    _tintByRace.Add(race.RaceId, race.BelongingColor);
                }
            }
        }

        public Color TintFor(string raceId)
        {
            return !string.IsNullOrEmpty(raceId) && _tintByRace.TryGetValue(raceId, out var tint)
                ? tint
                : Color.white;
        }
    }
}
