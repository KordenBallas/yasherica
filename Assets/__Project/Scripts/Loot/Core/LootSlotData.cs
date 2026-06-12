using System;

namespace Loot.Core
{
    /// <summary>
    /// Plain-data snapshot of one enemy loot slot. Each slot rolls independently
    /// against its probability; bonus slots mark rare high-value extras.
    /// </summary>
    public class LootSlotData
    {
        public string ArtifactId { get; }
        public float Probability { get; }
        public int QuantityMin { get; }
        public int QuantityMax { get; }
        public bool IsBonus { get; }

        public LootSlotData(string artifactId, float probability, int quantityMin, int quantityMax, bool isBonus)
        {
            if (string.IsNullOrEmpty(artifactId))
            {
                throw new ArgumentException("Artifact id must not be null or empty.", nameof(artifactId));
            }

            ArtifactId = artifactId;
            Probability = probability;
            QuantityMin = quantityMin;
            QuantityMax = quantityMax;
            IsBonus = isBonus;
        }
    }
}
