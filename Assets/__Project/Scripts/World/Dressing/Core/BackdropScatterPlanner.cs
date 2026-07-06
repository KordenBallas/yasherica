using System;
using System.Collections.Generic;
using Loot.Core;
using Narrative.Director.Core;

namespace World.Dressing.Core
{
    /// <summary>
    /// Plans the distant low-poly scatter behind the traversal field (world-backdrop-fill brief,
    /// Track E4): the world's forward X axis is divided into fixed slots, each slot draws its
    /// items from a private per-slot seeded stream — so contiguous half-open spans (the streaming
    /// windows, replayed identically on restore) emit every slot exactly once and the same run
    /// seed always yields the same horizon. Low density by design: a handful of reused silhouettes
    /// reads as a horizon (brief FR6). Pure C#; no UnityEngine.
    /// </summary>
    public sealed class BackdropScatterPlanner
    {
        private const string SeedContext = "backdrop-scatter";

        /// <summary>Forward length of one scatter slot, world units.</summary>
        public const float SlotLength = 25f;

        /// <summary>Depth band the scatter occupies along the CAMERA's depth axis — behind the
        /// route corridor and the routing landmarks, fully in FRONT of the hero-anchored ridge
        /// strips (near ridge ≈ 180) and the sky band (340), so silhouettes never hide behind
        /// the painted horizon.</summary>
        public const float BandNearZ = 80f;
        public const float BandFarZ = 160f;

        public IReadOnlyList<BackdropScatterPlacement> PlanSpan(
            float fromX,
            float toXExclusive,
            IReadOnlyList<BackdropEntryData> entries,
            float itemsPer100Units,
            int runSeed)
        {
            var placements = new List<BackdropScatterPlacement>();
            if (toXExclusive <= fromX || entries == null || entries.Count == 0 || itemsPer100Units <= 0f)
            {
                return placements;
            }

            int totalWeight = 0;
            foreach (var entry in entries)
            {
                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0)
            {
                return placements;
            }

            // A slot belongs to the span that contains its START — the same half-open ownership
            // rule the landmark scan uses, so streaming windows never double-fill a slot.
            int firstSlot = (int)Math.Floor(fromX / SlotLength);
            int lastSlot = (int)Math.Floor((toXExclusive - 0.0001f) / SlotLength);
            float expectedPerSlot = itemsPer100Units * SlotLength / 100f;

            for (int slot = Math.Max(0, firstSlot); slot <= lastSlot; slot++)
            {
                float slotStart = slot * SlotLength;
                if (slotStart < fromX || slotStart >= toXExclusive)
                {
                    continue;
                }

                var rng = new DeterministicRandom(
                    unchecked((ulong)LootSeed.Derive(runSeed, $"{SeedContext}:{slot}")));

                int count = (int)Math.Floor(expectedPerSlot);
                float fraction = expectedPerSlot - count;
                if (NextFloat(rng) < fraction)
                {
                    count++;
                }

                for (int i = 0; i < count; i++)
                {
                    int entryIndex = DrawWeighted(entries, totalWeight, rng);
                    var entry = entries[entryIndex];
                    float x = slotStart + NextFloat(rng) * SlotLength;
                    float z = BandNearZ + NextFloat(rng) * (BandFarZ - BandNearZ);
                    float yaw = NextFloat(rng) * 360f;
                    float scale = entry.ScaleMin + (entry.ScaleMax - entry.ScaleMin) * NextFloat(rng);
                    placements.Add(new BackdropScatterPlacement(entryIndex, x, z, yaw, scale));
                }
            }

            return placements;
        }

        private static int DrawWeighted(
            IReadOnlyList<BackdropEntryData> entries, int totalWeight, IRandomSource rng)
        {
            int roll = rng.NextInt(totalWeight);
            for (int i = 0; i < entries.Count; i++)
            {
                roll -= entries[i].Weight;
                if (roll < 0)
                {
                    return i;
                }
            }

            return entries.Count - 1;
        }

        private static float NextFloat(IRandomSource rng) => rng.NextInt(10000) / 10000f;
    }
}
