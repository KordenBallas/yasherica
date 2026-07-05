using System.Collections.Generic;
using Core.Logging;

namespace World.Races.Core
{
    /// <summary>
    /// Pure projection of the equipped body onto per-race acceptance tiers
    /// (race-roster-and-passport.md FR4): tier = count of that race's tagged parts equipped,
    /// clamped to 0 = outsider / 1 = tolerated / 2+ = kin. Kindless parts (empty race id) never
    /// count; races never exclude each other (FR5). Deterministic — same equipped body, same
    /// tiers (FR8).
    /// </summary>
    public static class RaceAcceptanceCalculator
    {
        /// <summary>Wearing one marker part: "one of us, but a freak".</summary>
        public const int ToleratedTier = 1;

        /// <summary>Wearing this many (or more) marker parts reads as kin; tiers clamp here.</summary>
        public const int KinTier = 2;

        /// <summary>
        /// Computes the tier for every roster race (0 for races with no equipped marker).
        /// <paramref name="equippedRaceIds"/> is one race id per equipped part; empty/null entries
        /// are kindless. Ids not in the roster are ignored with a warning (an authoring typo on a
        /// part must never gate narrative).
        /// </summary>
        public static IReadOnlyDictionary<string, int> ComputeTiers(
            IEnumerable<string> equippedRaceIds, IRaceRoster roster, IGameLogger logger = null)
        {
            var tiers = new Dictionary<string, int>(System.StringComparer.Ordinal);
            if (roster == null)
            {
                return tiers;
            }

            for (int i = 0; i < roster.All.Count; i++)
            {
                tiers[roster.All[i].Id] = 0;
            }

            if (equippedRaceIds == null)
            {
                return tiers;
            }

            foreach (var raceId in equippedRaceIds)
            {
                if (string.IsNullOrEmpty(raceId))
                {
                    continue; // kindless
                }

                if (!tiers.TryGetValue(raceId, out var count))
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[RaceAcceptanceCalculator] Part tagged with unknown race id '{raceId}' ignored.");
                    continue;
                }

                if (count < KinTier)
                {
                    tiers[raceId] = count + 1;
                }
            }

            return tiers;
        }
    }
}
