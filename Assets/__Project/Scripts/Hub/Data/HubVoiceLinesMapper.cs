using System;
using System.Collections.Generic;
using Core.Logging;
using Hub.Core;

namespace Hub.Data
{
    /// <summary>
    /// The only bridge from the <see cref="HubVoiceLinesConfig"/> SO to the UnityEngine-free
    /// <see cref="CauldronVoiceLines"/> record (CLAUDE.md §7). Blank lines and empty race ids are
    /// dropped; a missing asset maps to <see cref="CauldronVoiceLines.Empty"/> (a quiet cauldron,
    /// never a crash).
    /// </summary>
    public static class HubVoiceLinesMapper
    {
        public static CauldronVoiceLines ToLines(HubVoiceLinesConfig config, IGameLogger logger = null)
        {
            if (config == null)
            {
                logger?.Warning(LogCategory.Core,
                    "[HubVoiceLinesMapper] No HubVoiceLinesConfig wired; the cauldron stays quiet on the Hub.");
                return CauldronVoiceLines.Empty;
            }

            var byRace = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (var pool in config.PartPickedByRace)
            {
                if (pool == null || string.IsNullOrEmpty(pool.RaceId))
                {
                    continue;
                }

                if (byRace.ContainsKey(pool.RaceId))
                {
                    logger?.Warning(LogCategory.Core,
                        $"[HubVoiceLinesMapper] Duplicate race pool '{pool.RaceId}'; first authored wins.");
                    continue;
                }

                var lines = Clean(pool.Lines);
                if (lines.Count > 0)
                {
                    byRace.Add(pool.RaceId, lines);
                }
            }

            return new CauldronVoiceLines(
                byRace,
                Clean(config.PartPickedGeneric),
                Clean(config.NoPartAvailable),
                Clean(config.Launch),
                Clean(config.DeathReturn),
                Clean(config.HeatDare),
                Clean(config.HeatSealed),
                Clean(config.HeatDeclined));
        }

        private static List<string> Clean(IReadOnlyList<string> lines)
        {
            var cleaned = new List<string>(lines != null ? lines.Count : 0);
            if (lines == null)
            {
                return cleaned;
            }

            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    cleaned.Add(line.Trim());
                }
            }

            return cleaned;
        }
    }
}
