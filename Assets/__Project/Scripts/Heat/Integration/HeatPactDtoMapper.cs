using System.Collections.Generic;
using Core.Persistence;
using Heat.Core;

namespace Heat.Integration
{
    /// <summary>
    /// Persistence DTO ↔ Core pact bridge. Loading goes through <see cref="HeatPact.From"/>, so a
    /// stale save against a re-authored menu degrades gracefully (unknown ids drop, ranks clamp)
    /// and the total is always recomputed — never trusted from disk (FR12).
    /// </summary>
    public static class HeatPactDtoMapper
    {
        public static HeatPact ToPact(HeatSettings settings, IReadOnlyList<HeatPactEntryDto> entries)
        {
            if (settings == null || entries == null || entries.Count == 0)
            {
                return HeatPact.None;
            }

            var pairs = new List<KeyValuePair<string, int>>(entries.Count);
            foreach (var entry in entries)
            {
                if (entry != null)
                {
                    pairs.Add(new KeyValuePair<string, int>(entry.ModifierId, entry.Rank));
                }
            }

            return HeatPact.From(settings, pairs);
        }

        public static List<HeatPactEntryDto> ToDtos(HeatPact pact)
        {
            var dtos = new List<HeatPactEntryDto>();
            if (pact == null)
            {
                return dtos;
            }

            foreach (var entry in pact.Ranks)
            {
                dtos.Add(new HeatPactEntryDto { ModifierId = entry.ModifierId, Rank = entry.Rank });
            }

            return dtos;
        }
    }
}
