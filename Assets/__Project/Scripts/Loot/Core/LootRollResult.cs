using System;

namespace Loot.Core
{
    /// <summary>
    /// One rolled loot outcome: which artifact, how many, and whether it came
    /// from a bonus slot.
    /// </summary>
    public class LootRollResult
    {
        public string ArtifactId { get; }
        public int Quantity { get; }
        public bool IsBonus { get; }

        public LootRollResult(string artifactId, int quantity, bool isBonus)
        {
            if (string.IsNullOrEmpty(artifactId))
            {
                throw new ArgumentException("Artifact id must not be null or empty.", nameof(artifactId));
            }

            ArtifactId = artifactId;
            Quantity = quantity;
            IsBonus = isBonus;
        }
    }
}
