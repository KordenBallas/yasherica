using System.Collections.Generic;
using Mutation.Core;
using Mutation.Data.Definitions;

namespace Mutation.Data
{
    /// <summary>
    /// The only bridge from an authored <see cref="ArchetypePartSetDefinition"/> into the
    /// UnityEngine-free <see cref="MutationOption"/> Core records. Pure adapter: copies fields and
    /// stamps each option with its owning archetype id; the Sprite icon stays in the Data layer
    /// (served separately by the catalog) so Core holds no Unity types.
    /// </summary>
    public static class MutationOptionMapper
    {
        public static IReadOnlyList<MutationOption> ToOptions(ArchetypePartSetDefinition set)
        {
            if (set == null)
            {
                return System.Array.Empty<MutationOption>();
            }

            var options = new List<MutationOption>(set.Options.Count);
            foreach (var entry in set.Options)
            {
                if (entry == null)
                {
                    continue;
                }

                options.Add(new MutationOption(
                    entry.SlotId,
                    entry.PartId,
                    set.ArchetypeId,
                    entry.DisplayName));
            }

            return options;
        }
    }
}
