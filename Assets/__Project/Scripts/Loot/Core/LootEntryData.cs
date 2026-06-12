using System;
using System.Collections.Generic;

namespace Loot.Core
{
    /// <summary>
    /// Plain-data snapshot of one weighted loot table entry.
    /// The player-gating fields (MinPlayerLevel, RequiredAchievements, RequiredPastQuests)
    /// are carried through for future progression filters and are not enforced yet.
    /// </summary>
    public class LootEntryData
    {
        public string ArtifactId { get; }
        public float Weight { get; }
        public IReadOnlyList<string> BiasTags { get; }
        public int MinPlayerLevel { get; }
        public IReadOnlyList<string> RequiredAchievements { get; }
        public IReadOnlyList<string> RequiredPastQuests { get; }

        public LootEntryData(
            string artifactId,
            float weight,
            IReadOnlyList<string> biasTags = null,
            int minPlayerLevel = 0,
            IReadOnlyList<string> requiredAchievements = null,
            IReadOnlyList<string> requiredPastQuests = null)
        {
            if (string.IsNullOrEmpty(artifactId))
            {
                throw new ArgumentException("Artifact id must not be null or empty.", nameof(artifactId));
            }

            ArtifactId = artifactId;
            Weight = weight;
            BiasTags = biasTags ?? Array.Empty<string>();
            MinPlayerLevel = minPlayerLevel;
            RequiredAchievements = requiredAchievements ?? Array.Empty<string>();
            RequiredPastQuests = requiredPastQuests ?? Array.Empty<string>();
        }
    }
}
