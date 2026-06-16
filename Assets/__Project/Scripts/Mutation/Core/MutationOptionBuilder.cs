using System;
using System.Collections.Generic;

namespace Mutation.Core
{
    /// <summary>
    /// Default <see cref="IMutationOptionBuilder"/>: walks the dominant archetypes in order and the
    /// options each one offers in authored order, deduping by part id (first archetype wins) and
    /// excluding already-equipped parts, until the cap is reached. No LINQ, no allocations beyond the
    /// result list and the dedupe set; fully deterministic for repeatable choices and tests.
    /// </summary>
    public sealed class MutationOptionBuilder : IMutationOptionBuilder
    {
        public IReadOnlyList<MutationOption> Build(
            IReadOnlyList<string> dominantArchetypeIds,
            IMutationOptionProvider options,
            ISet<string> equippedPartIds,
            int maxOptions)
        {
            if (dominantArchetypeIds == null || options == null || maxOptions <= 0)
            {
                return Array.Empty<MutationOption>();
            }

            var chosen = new List<MutationOption>(maxOptions);
            var seenParts = new HashSet<string>(StringComparer.Ordinal);

            for (int a = 0; a < dominantArchetypeIds.Count && chosen.Count < maxOptions; a++)
            {
                var archetypeId = dominantArchetypeIds[a];
                var candidates = options.OptionsFor(archetypeId);
                if (candidates == null)
                {
                    continue;
                }

                for (int o = 0; o < candidates.Count && chosen.Count < maxOptions; o++)
                {
                    var option = candidates[o];
                    if (option == null || string.IsNullOrEmpty(option.PartId))
                    {
                        continue;
                    }

                    // Dedupe a part shared by two archetypes, and never re-offer an equipped part.
                    if (!seenParts.Add(option.PartId))
                    {
                        continue;
                    }

                    if (equippedPartIds != null && equippedPartIds.Contains(option.PartId))
                    {
                        continue;
                    }

                    chosen.Add(option);
                }
            }

            return chosen;
        }
    }
}
