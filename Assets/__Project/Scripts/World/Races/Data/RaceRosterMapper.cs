using System.Collections.Generic;
using Core.Logging;
using World.Races.Core;

namespace World.Races.Data
{
    /// <summary>
    /// The only bridge from the <see cref="RaceDefinition"/> SOs to the UnityEngine-free
    /// <see cref="IRaceRoster"/> Core catalog (CLAUDE.md §7). Entries with no id are skipped with a
    /// warning; duplicate ids are resolved first-authored-wins inside <see cref="RaceRoster"/>.
    /// An empty roster is valid (the world simply has no races yet).
    /// </summary>
    public static class RaceRosterMapper
    {
        public static IRaceRoster ToRoster(IReadOnlyList<RaceDefinition> definitions, IGameLogger logger = null)
        {
            var races = new List<RaceData>();
            if (definitions == null || definitions.Count == 0)
            {
                logger?.Warning(LogCategory.Narrative,
                    "[RaceRosterMapper] No RaceDefinition assets wired; the race roster is empty.");
                return new RaceRoster(races, logger);
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(definition.RaceId))
                {
                    logger?.Warning(LogCategory.Narrative,
                        $"[RaceRosterMapper] Race asset '{definition.name}' has no race id; skipped.");
                    continue;
                }

                var displayName = string.IsNullOrEmpty(definition.DisplayName)
                    ? definition.name
                    : definition.DisplayName;
                races.Add(new RaceData(definition.RaceId, displayName, definition.HomeBiome));
            }

            return new RaceRoster(races, logger);
        }
    }
}
