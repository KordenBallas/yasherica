using System.Collections.Generic;
using Mutation.Core;
using Mutation.Data.Definitions;

namespace Mutation.Data
{
    /// <summary>
    /// The only bridge from authored <see cref="ArchetypeWeight"/> data into the
    /// UnityEngine-free <see cref="ArtifactArchetypeProfile"/> Core record.
    /// Pure adapter: all aggregation rules live in <see cref="ArtifactArchetypeProfile.Create"/>.
    /// </summary>
    public static class ArtifactArchetypeMapper
    {
        public static ArtifactArchetypeProfile ToProfile(IEnumerable<ArchetypeWeight> weights)
        {
            if (weights == null)
            {
                return ArtifactArchetypeProfile.Empty;
            }

            return ArtifactArchetypeProfile.Create(ToEntries(weights));
        }

        private static IEnumerable<KeyValuePair<string, float>> ToEntries(IEnumerable<ArchetypeWeight> weights)
        {
            foreach (var weight in weights)
            {
                if (weight == null)
                {
                    continue;
                }

                yield return new KeyValuePair<string, float>(weight.ArchetypeId, weight.Weight);
            }
        }
    }
}
